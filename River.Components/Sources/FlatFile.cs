using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace River.Components.Sources
{
    public class FlatFile : Source
    {
        public Contexts.Sources.FlatFile _context;

        public FlatFile(Contexts.Sources.FlatFile source, TransformBlock<Dictionary<string, object>, Dictionary<string, object>> bed)
            : base(source, bed)
        {
            _context = source;
        }

        internal override IEnumerable<Dictionary<string, object>> GetDrops()
        {
            using (var reader = new System.IO.StreamReader(_context.Location))
            {
                string line = null;

                string[] columns = null;

                while ((line = reader.ReadLine()) != null)
                {
                    if (columns == null) columns = ParseLine(line, _context.Delimiters);
                    else
                        yield return MakeRowObj(ParseLine(line, _context.Delimiters), columns);
                }
            }
        }

        private Dictionary<string, object> MakeRowObj(string[] values, string[] headers)
        {
            if (headers.Length != values.Length)
                throw new ArgumentOutOfRangeException("values"
                    , string.Format("headers:{0}, values:{1}", headers.Length, values.Length)
                    , "The number of values did not match the number of headers");

            var rowObj = new Dictionary<string, object>();

            for (var i = 0; i < headers.Length; i++ )
                rowObj.Add(headers[i], values[i]);

            return rowObj;
        }

        private string[] ParseLine(string line, char[] delimiters)
        {
            return line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
