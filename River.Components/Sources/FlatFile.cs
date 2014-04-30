using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Components.Sources
{
    public class FlatFile : Source
    {
        public override IEnumerable<Dictionary<string, object>> GetRows(Contexts.Sources.Source source)
        {
            var context = source as Contexts.Sources.FlatFile;

            using (var reader = new System.IO.StreamReader(context.Location))
            {
                string line = null;

                IEnumerable<string> columns = null;

                while ((line = reader.ReadLine()) != null)
                {
                    if (columns == null) columns = ParseLine(line, context.Delimiters);
                    else
                        yield return MakeRowObj(ParseLine(line, context.Delimiters), columns);
                }
            }
        }

        private Dictionary<string, object> MakeRowObj(IEnumerable<string> values, IEnumerable<string> headers)
        {
            if (headers.Count() != values.Count())
                throw new ArgumentOutOfRangeException("values"
                    , string.Format("headers:{0}, values:{1}", headers.Count(), values.Count())
                    , "The number of values did not match the number of headers");

            var rowObj = new Dictionary<string, object>();

            foreach (var header in headers)
                foreach (var value in values)
                    rowObj.Add(header, value);

            return rowObj;
        }

        private IEnumerable<string> ParseLine(string line, char[] delimiters)
        {
            return line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
