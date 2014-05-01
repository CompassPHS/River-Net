using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Components.Sources
{
    public class Database : Source
    {
        public override IEnumerable<Dictionary<string, object>> GetRows(Contexts.Sources.Source source)
        {
            var context = source as Contexts.Sources.Database;

            using (var connection = new SqlConnection(context.ConnectionString))
            {
                connection.Open();

                using (var cmd = new SqlCommand(context.Command, connection))
                {
                    cmd.CommandTimeout = context.CommandTimeout;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rowObj = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var data = reader[i];

                                if (context.SuppressNulls && data == DBNull.Value) continue;

                                ParseColumn(reader.GetName(i), data, rowObj);
                            }

                            yield return rowObj;
                        }
                    }
                }
            }
        }

        private void ParseColumn(string column, object data, Dictionary<string, object> parentObj)
        {
            // First child is property
            if ((column.IndexOf('.') > -1 && column.IndexOf('[') > -1 && column.IndexOf('.') < column.IndexOf('['))
                || (column.IndexOf('.') > -1 && column.IndexOf(']') == -1))
            {
                var idx = column.IndexOf('.');
                var name = column.Substring(0, idx);

                if (!parentObj.ContainsKey(name))
                    parentObj[name] = new Dictionary<string, object>();

                ParseColumn(column.Substring(idx + 1).Trim(), data, (parentObj[name] as Dictionary<string, object>));
            }
            // First child is array of primitives
            else if (column.IndexOf('[') > -1 && column.IndexOf(']') == column.IndexOf('[') + 1)
            {
                var idx = column.IndexOf('[');
                var name = column.Substring(0, idx);

                if (!parentObj.ContainsKey(name))
                    parentObj[name] = new List<object>() { data };
                else
                {
                    var list = parentObj[name] as List<object>;
                    if (!list.Contains(data)) list.Add(data);
                }
            }
            // First child is array of objects
            else if ((column.IndexOf('[') > -1 && column.IndexOf('.') > -1 && column.IndexOf('[') < column.IndexOf('.'))
                || (column.IndexOf('[') > -1 && column.IndexOf('.') == -1))
            {
                var idx = column.IndexOf('[');
                var name = column.Substring(0, idx);

                var childName = column.Substring(idx + 1);

                if ((childName.IndexOf(']') > -1 && childName.IndexOf('[') > -1 && childName.IndexOf(']') < childName.IndexOf('['))
                    || (childName.IndexOf(']') > -1 && childName.IndexOf('[') == -1))
                {
                    var remove = childName.IndexOf(']');
                    childName = childName.Substring(0, remove) + childName.Substring(remove + 1, childName.Length - remove - 1);
                }

                if (!parentObj.ContainsKey(name))
                    parentObj[name] = new List<Dictionary<string, object>>() { new Dictionary<string, object>() };

                ParseColumn(childName, data, (parentObj[name] as List<Dictionary<string, object>>)[0] as Dictionary<string, object>);

            }
            // No children
            else
            {
                parentObj[column] = data;
            }
        }
    }
}
