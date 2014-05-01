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
    public class River
    {
        private RiverContext _riverContext;
        Sources.Source _source;
        Nest.ElasticClient _client;
        ILog log = Common.Logging.LogManager.GetCurrentClassLogger();

        public River(RiverContext riverContext)
        {
            _riverContext = riverContext;
            _source = Sources.Source.GetSource(riverContext.Source);

            _client = new Nest.ElasticClient(new Nest.ConnectionSettings(new Uri(_riverContext.Destination.Url)));
        }

        private void BulkPushToElasticsearch(string body)
        {
            var response = _client.Raw.BulkPut(_riverContext.Destination.Index
                , _riverContext.Destination.Type
                , body
                , null);

            log.Info(response);            
        }

        StringBuilder sb = new StringBuilder();
        int count = 0;
        int start = System.Environment.TickCount;

        private void PushObj(Dictionary<string, object> curObj, bool pushNow)
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var index = _riverContext.Destination.Index;
            var type = _riverContext.Destination.Type;
            sb.Append("{ \"index\" : { \"_index\" : \"" + index + "\", \"_type\" : \"" + type + "\"");
            if (curObj.ContainsKey("_id")) sb.Append(", \"_id\" : \"" + curObj["_id"] + "\"");
            if (curObj.ContainsKey("_parent")) sb.Append(", \"_parent\" : \"" + curObj["_parent"] + "\"");
            sb.Append(" } }");
            sb.Append("\n");
            sb.Append(JsonConvert.SerializeObject(curObj, settings));
            sb.Append("\n");

            count++;

            if (pushNow || count % _riverContext.MaxBulkSize == 0)
            {
                BulkPushToElasticsearch(sb.ToString());
                sb.Clear();
                var end = System.Environment.TickCount;
                log.Info(string.Format("{0} has taken {1}s", count, (end - start) / 1000));
            }
        }

        public void Flow()
        {
            log.Info(string.Format("Starting river {0}", _riverContext.Name));
            Dictionary<string, object> curObj = null;

            try
            {
                foreach (var rowObj in _source.GetRows(_riverContext.Source))
                {
                    try
                    {
                        if (curObj == null)
                        {
                            curObj = new Dictionary<string, object>();
                        }
                        else if (!curObj.ContainsKey("_id") || curObj["_id"].ToString() != rowObj["_id"].ToString())
                        {
                            //push curObj
                            PushObj(curObj, false);

                            //now make a new obj
                            curObj = new Dictionary<string, object>();
                        }

                        Merge(rowObj, curObj);
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format("Error river {0}", _riverContext.Name), e);
                    }
                }
            }
            catch (Exception e)
            {                
                log.Error(string.Format("Error river {0}", _riverContext.Name), e);
            }

            if (curObj != null) PushObj(curObj, true);

            log.Info(string.Format("Completed river {0}", _riverContext.Name));
        }


        private void Merge(Dictionary<string, object> src, Dictionary<string, object> dest)
        {
            foreach (var skvp in src)
            {
                if (!dest.ContainsKey(skvp.Key))
                {
                    dest.Add(skvp.Key, skvp.Value);
                }
                else if (skvp.Value.GetType() == typeof(Dictionary<string, object>))
                {
                    Merge(skvp.Value as Dictionary<string, object>, dest[skvp.Key] as Dictionary<string, object>);
                }
                else if (skvp.Value.GetType() == typeof(List<Dictionary<string, object>>))
                {
                    var srcList = skvp.Value as List<Dictionary<string, object>>;
                    var destList = dest[skvp.Key] as List<Dictionary<string, object>>;

                    foreach (var srcChild in srcList)
                    {
                        Dictionary<string, object> destMatch = null;
                        foreach (var destChild in destList)
                        {
                            if (destChild.ContainsKey("_id") && srcChild.ContainsKey("_id")
                                && destChild["_id"].ToString() == srcChild["_id"].ToString())
                            {
                                destMatch = destChild;
                                break;
                            }
                        }

                        if (destMatch != null) Merge(srcChild, destMatch);
                        else destList.Add(srcChild);
                    }
                }
                else if (skvp.Value.GetType() == typeof(List<object>))
                {
                    var srcList = skvp.Value as List<object>;
                    var destList = dest[skvp.Key] as List<object>;

                    foreach (var srcChild in srcList)
                    {
                        if (!destList.Contains(srcChild))
                            destList.Add(srcChild);
                    }
                }
                else
                {
                    dest[skvp.Key] = skvp.Value;
                }
            }
        }

    }
}
