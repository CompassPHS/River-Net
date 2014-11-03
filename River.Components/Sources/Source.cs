using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Components.Sources
{
    public abstract class Source
    {
        public abstract IEnumerable<Dictionary<string, object>> GetRows(Contexts.Sources.Source source);

        public static Source GetSource(Contexts.Sources.Source source)
        {
            var contextType = source.GetType();
            if (contextType == typeof(Contexts.Sources.Database))
                return new Database();
            else if(contextType == typeof(Contexts.Sources.FlatFile))
                return new FlatFile();
            else
                throw new ArgumentException();
        }
    }

    
}
