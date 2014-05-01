using Newtonsoft.Json;
using River.Components.Contexts.Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace River.Components.Contexts
{
    [Serializable]
    [JsonObject]
    public class RiverContext
    {
        public string Name { get; set; }

        public Source Source { get; set; }

        public Destination Destination { get; set; }

        public string Cron { get; set; }
        
        public RiverContext()
        {
            
        }
    }    
}
