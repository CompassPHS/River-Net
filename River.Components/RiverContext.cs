using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Components
{
    public class RiverContext
    {
        public string Name { get; set; }

        public Source Source { get; set; }

        public Destination Destination { get; set; }

        public string Cron { get; set; }

        public int MaxBulkSize { get; set; }

        public bool SuppressNulls { get; set; }

        public RiverContext()
        {
            MaxBulkSize = 100;
        }
    }

    public class Source
    {
        public string Server { get; set; }

        public string Database { get; set; }

        public bool Trusted { get; set; } // Ignores user and password

        public string User { get; set; }

        public string Password { get; set; }

        public Sql Sql { get; set; }
    }

    public class Destination
    {
        public string Url { get; set; }

        public string Index { get; set; }

        public string Type { get; set; }

    }

    public class Sql
    {
        public string Command { get; set; }

        public bool IsProc { get; set; }
    }
}
