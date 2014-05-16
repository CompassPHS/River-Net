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

        public Mouth(Contexts.Destination destination)
        {
            _destination = destination;
            _client = new Nest.ElasticClient(new Nest.ConnectionSettings(new Uri(destination.Url)));
        }

        private void BulkPushToElasticsearch(string body)
        {
            var connectionStatus = _client.Raw.BulkPut(_destination.Index
                , _destination.Type
                , body
                , null);

            if (connectionStatus.Success)
            {
                log.Debug(connectionStatus);
                log.Info(string.Format("Result:{0}", connectionStatus.Success));
            }
            else
            {
                log.Error(connectionStatus.Error.ExceptionMessage);
            }
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