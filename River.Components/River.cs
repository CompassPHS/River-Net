using Newtonsoft.Json;
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
        //String connectionString = ConfigurationManager.ConnectionStrings["CDW"].ConnectionString;
        private RiverContext _riverContext;
        Nest.ElasticClient _client;

        public River(RiverContext riverContext)
        {
            _riverContext = riverContext;
            _client = new Nest.ElasticClient(new Nest.ConnectionSettings(new Uri(_riverContext.Destination.Url)));
        }

        private void BulkPushToElasticsearch(string body)
        {
            var response = _client.Raw.BulkPut(_riverContext.Destination.Index
                , _riverContext.Destination.Type
                , body
                , null);

            Console.WriteLine(response);
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
            sb.Append("{ \"index\" : { \"_index\" : \"" + index + "\", \"_type\" : \"" + type + "\", \"_id\" : \"" + curObj["_id"] + "\" } }");
            sb.Append("\n");
            sb.Append(JsonConvert.SerializeObject(curObj, settings));
            sb.Append("\n");

            count++;

            if (pushNow || count % _riverContext.MaxBulkSize == 0)
            {
                BulkPushToElasticsearch(sb.ToString());
                sb.Clear();
                var end = System.Environment.TickCount;
                Console.WriteLine("{0} has taken {1}s", count, (end - start) / 1000);
            }
        }

        public void Flow()
        {
            Dictionary<string, object> curObj = null;

            foreach (var rowObj in GetRows(_riverContext.Source))
            {
                try
                {
                    if (curObj == null)
                    {
                        curObj = new Dictionary<string, object>();
                    }
                    else if (curObj.ContainsKey("_id") && curObj["_id"].ToString() != rowObj["_id"].ToString())
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
                    Console.WriteLine(e);
                }
            }

            if (curObj != null) PushObj(curObj, true);
        }

        private IEnumerable<Dictionary<string, object>> GetRows(Source source)
        {
            //Data Source=cphs-sqltest-01;Initial Catalog=CDW;Persist Security Info=True;User ID=usr_lineitem;Password=l1n3!t3m
            var connectionString = String.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True; User ID={2};Password={3}"
                , source.Server //"cphs-sqltest-01"
                , source.Database //"CDW"
                , source.User //"usr_lineitem"
                , source.Password //"l1n3!t3m"
            );

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var cmd = new SqlCommand(source.Sql.Command, connection))
                {
                    if (source.Sql.IsProc) cmd.CommandType = CommandType.StoredProcedure;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rowObj = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var data = reader[i];

                                if (_riverContext.SuppressNulls && data == DBNull.Value) continue;

                                ParseColumn(reader.GetName(i), data, rowObj);
                            }

                            yield return rowObj;
                        }
                    }
                }
            }
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
                else
                {
                    dest[skvp.Key] = skvp.Value;
                }
            }
        }

        private void ParseColumn(string column, object data, Dictionary<string, object> parentObj)
        {
            if (column.IndexOf('.') > -1
                && (column.IndexOf('.') < column.LastIndexOf(']')
                    || column.IndexOf(']') == -1))
            {
                var idx = column.IndexOf('.');
                var name = column.Substring(0, idx);

                if (!parentObj.ContainsKey(name))
                    parentObj[name] = new Dictionary<string, object>();

                ParseColumn(column.Substring(idx + 1), data, (parentObj[name] as Dictionary<string, object>));
            }
            else if (column.LastIndexOf(']') > -1
                && (column.LastIndexOf(']') < column.IndexOf('.')
                    || column.IndexOf('.') == -1))
            {
                var idx = column.IndexOf('[');
                var name = column.Substring(0, idx);

                if (!parentObj.ContainsKey(name))
                    parentObj[name] = new List<Dictionary<string, object>>() { new Dictionary<string, object>() };

                ParseColumn(column.Substring(idx + 1, column.LastIndexOf(']') - idx - 1), data, (parentObj[name] as List<Dictionary<string, object>>)[0] as Dictionary<string, object>);
            }
            else
            {
                parentObj[column] = data;
            }
        }

    }
}
