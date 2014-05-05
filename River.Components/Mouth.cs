using Common.Logging;
using Newtonsoft.Json;
using River.Components.Contexts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace River.Components
{
    public class Mouth
    {
        ILog log = Common.Logging.LogManager.GetCurrentClassLogger();

        Contexts.Destination _destination;
        Nest.ElasticClient _client;
        SemaphoreSlim _executingPushes;

        BatchBlock<Dictionary<string, object>> Batch { get; set; }
        ActionBlock<Dictionary<string, object>[]> Push { get; set; }
        List<Task> PushTasks { get; set; }

        public Mouth(Contexts.Destination destination, TransformBlock<Dictionary<string, object>, Dictionary<string, object>> bed)
        {
            _destination = destination;
            _client = new Nest.ElasticClient(new Nest.ConnectionSettings(new Uri(destination.Url)));
            CreateFlow(bed);
        }

        private void CreateFlow(TransformBlock<Dictionary<string, object>, Dictionary<string, object>> bed)
        {
            // Dataflow: Batch -> Push
            Batch = new BatchBlock<Dictionary<string, object>>(_destination.MaxBulkSize);
            Push = new ActionBlock<Dictionary<string, object>[]>(bucket =>
            {
                var sb = new StringBuilder();

                foreach (var drop in bucket)
                {
                    sb.Append(Channel(drop));
                }

                BulkPushToElasticsearch(sb.ToString());
            }, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = _destination.MaxConcurrentRequests });

            bed.LinkTo(Batch);
            Batch.LinkTo(Push);

            bed.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)Batch).Fault(t.Exception);
                else Batch.Complete();
            });

            Batch.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)Push).Fault(t.Exception);
                else Push.Complete();
            });
        }

        private void BulkPushToElasticsearch(string body)
        {
            if (!_indexCreated) EagerCreateIndex(_destination);
            var response = _client.Raw.BulkPut(_destination.Index
                , _destination.Type
                , body
                , null);
            ProcessBulkPushToElasticsearchResult(response);
        }

        private void ProcessBulkPushToElasticsearchResult(Nest.ConnectionStatus connectionStatus)
        {
            log.Debug(connectionStatus.Result);
            log.Info(string.Format("Result: {0}", connectionStatus.Success));
        }

        int start = System.Environment.TickCount;

        bool _indexCreated = false;

        public string Channel(Dictionary<string, object> curObj)
        {
            var sb = new StringBuilder();

            var settings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var index = _destination.Index;
            var type = _destination.Type;
            sb.Append("{ \"index\" : { \"_index\" : \"" + index + "\", \"_type\" : \"" + type + "\"");
            if (curObj.ContainsKey("_id")) sb.Append(", \"_id\" : \"" + curObj["_id"] + "\"");
            if (curObj.ContainsKey("_parent")) sb.Append(", \"_parent\" : \"" + curObj["_parent"] + "\"");
            sb.Append(" } }");
            sb.Append("\n");
            sb.Append(JsonConvert.SerializeObject(curObj, settings));
            sb.Append("\n");

            return sb.ToString();
        }

        private void EagerCreateIndex(Destination destination)
        {
            try
            {
                var index = _client.CreateIndex(destination.Index, new Nest.IndexSettings());
                _indexCreated = true;

                if (destination.Mapping != null)
                {
                    _client.MapFluent(m =>
                    {
                        m.IndexName(destination.Index);
                        m.TypeName(destination.Type);
                        
                        if (destination.Mapping.Parent != null)
                            m.SetParent(destination.Mapping.Parent.Type);

                        return m;
                    });
                }

                var settings = new Nest.IndexSettings();
                settings.Add("refresh_interval", "-1");
                _client.UpdateSettings(destination.Index, settings);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }

        private void Productionalize(Destination destination)
        {
            var settings = new Nest.IndexSettings();
            settings.Add("refresh_interval", "1s");
            _client.UpdateSettings(destination.Index, settings);
        }

        internal void IsEmptied()
        {
            Push.Completion.Wait();
            Productionalize(_destination);
        }
    }
}