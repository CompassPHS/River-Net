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
using System.Threading.Tasks;

namespace River.Components
{
    public class Mouth
    {
        ILog log = Common.Logging.LogManager.GetCurrentClassLogger();

        Contexts.Destination _destination;
        Nest.ElasticClient _client;
        List<Task> _responses;

        public Mouth(Contexts.Destination destination)
        {
            _destination = destination;
            _client = new Nest.ElasticClient(new Nest.ConnectionSettings(new Uri(destination.Url)));
            _responses = new List<Task>();
        }

        private void BulkPushToElasticsearch(string body)
        {
            _responses.Add(_client.Raw.BulkPutAsync(_destination.Index
                , _destination.Type
                , body
                , null).ContinueWith(s => ProcessBulkPushToElasticsearchResult(s.Result)));
        }

        private void ProcessBulkPushToElasticsearchResult(Nest.ConnectionStatus connectionStatus)
        {
            if (connectionStatus.Success)
            {
                log.Info(connectionStatus);
            }
            else
            {
                log.Error(connectionStatus.Error.ExceptionMessage);
            }
        }

        public void Empty()
        {
            Task.WaitAll(_responses.ToArray());
        }

        StringBuilder sb = new StringBuilder();
        int count = 0;
        int start = System.Environment.TickCount;

        bool _indexCreated = false;

        public void PushObj(Dictionary<string, object> curObj, bool pushNow)
        {
            if (!_indexCreated) EagerCreateIndex(_destination);

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

            count++;

            if (pushNow || count % _destination.MaxBulkSize == 0)
            {
                BulkPushToElasticsearch(sb.ToString());
                sb.Clear();
                var end = System.Environment.TickCount;
                log.Info(string.Format("{0} has taken {1}s", count, (end - start) / 1000));
            }
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
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }
    }
}