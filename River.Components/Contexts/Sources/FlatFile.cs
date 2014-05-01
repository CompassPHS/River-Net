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
    public class FlatFile : Source
    {
        public override string Type
        {
            get { return "flatFile"; }
        }

        public string Location { get; set; }

        public char[] Delimiters { get; set; }
    }
}
