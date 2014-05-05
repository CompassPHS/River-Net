using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace River.Components.Sources
{
    public class Database : Source
    {
        public Contexts.Sources.Database _context;

        public Database(Contexts.Sources.Database source, TransformBlock<Dictionary<string, object>, Dictionary<string, object>> bed)
            : base(bed)
        {
            _context = source;
        }

        internal override IEnumerable<Dictionary<string, object>> GetDrops()
        {
            using (var connection = new SqlConnection(_context.ConnectionString))
            {
                connection.Open();

                using (var cmd = new SqlCommand(_context.Command, connection))
                {
                    cmd.CommandTimeout = _context.CommandTimeout;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rowObj = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var data = reader[i];

                                if (_context.SuppressNulls && data == DBNull.Value) continue;

                                rowObj.Add(reader.GetName(i), data);
                            }

                            yield return rowObj;
                        }
                    }
                }
            }
        }
    }
}
