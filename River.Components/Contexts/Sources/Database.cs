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
    public class Database : Source
    {
        public override string Type { get { return "database"; } }

        public string ConnectionString { get; set; }

        public int CommandTimeout { get; set; }

        public string Command { get; set; }

        public Database()
        {
            CommandTimeout = 30;
        }
    }
}
