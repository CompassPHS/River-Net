using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Components.Contexts
{
    [Serializable]
    [JsonObject]
    public class Destination
    {
        public string Url { get; set; }

        public string Index { get; set; }

        public string Type { get; set; }

        public int MaxBulkSize { get; set; }

        public int MaxConcurrentRequests { get; set; }

        public Mapping Mapping { get; set; }

        Destination()
        {
            MaxBulkSize = 100;
            MaxConcurrentRequests = 3;
        }
    }   

    [Serializable]
    [JsonObject]
    public class Mapping
    {
        public Parent Parent { get; set; }
    }

    [Serializable]
    [JsonObject]
    public class Parent
    {
        public string Type { get; set; }
    }
}
