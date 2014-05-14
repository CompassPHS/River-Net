using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Components.Contexts.Sources
{
    [Serializable]
    [JsonObject]
    public abstract class Source
    {
        public abstract string Type { get; }

        public bool SuppressNulls { get; set; }

        public int MaxWorkers { get; set; }

        protected Source()
        {
            SuppressNulls = true;
            MaxWorkers = 1;
        }
    }
}
