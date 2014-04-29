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
        public string ConnectionString { get; set; }

        public int CommandTimeout { get; set; }

        public string Command { get; set; }

        public Source()
        {
            CommandTimeout = 30;
        }
    }

    public class Destination
    {
        public string Url { get; set; }

        public string Index { get; set; }

        public string Type { get; set; }

    }   
}
