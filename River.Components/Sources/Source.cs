using Common.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace River.Components.Sources
{
    public abstract class Source
    {
        /// <summary>
        /// Factory to create source type
        /// </summary>
        /// <param name="source">Source context</param>
        /// <param name="bed">River bed to connect the source to</param>
        /// <returns>Source created from input context</returns>
        public static Source GetSource(Contexts.Sources.Source source,
            TransformBlock<Dictionary<string, object>, Dictionary<string, object>> bed)
        {
            var contextType = source.GetType();
            if (contextType == typeof(Contexts.Sources.Database))
                return new Database(source as Contexts.Sources.Database, bed);
            else if (contextType == typeof(Contexts.Sources.FlatFile))
                return new FlatFile(source as Contexts.Sources.FlatFile, bed);
            else
                throw new ArgumentException();
        }

        /// <summary>
        /// Gets "drops" from source, corresponding to specific rows in the source type.
        /// </summary>
        /// <returns>Yielded enumeration of header/value pairs</returns>
        internal abstract IEnumerable<Dictionary<string, object>> GetDrops();

        public Source(TransformBlock<Dictionary<string, object>, Dictionary<string, object>> bed)
        {
            CreateFlow(bed);
        }

        private TransformBlock<
                IEnumerable<Dictionary<string, object>>,
                IEnumerable<Dictionary<string, object>>> Parser { get; set; }
        private TransformBlock<
                IEnumerable<Dictionary<string, object>>,
                Dictionary<string, object>> Merger { get; set; }

        ILog log = Common.Logging.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates the data flow blocks and lays out the processing pipeline
        /// </summary>
        /// <param name="bed">River bed to connect the source flow to</param>
        private void CreateFlow(TransformBlock<Dictionary<string, object>, Dictionary<string, object>> bed)
        {
            Parser = new TransformBlock<
                IEnumerable<Dictionary<string, object>>,
                IEnumerable<Dictionary<string, object>>>(bucket =>
                {
                    return ParseBucket(bucket);
                });

            Merger = new TransformBlock<
                IEnumerable<Dictionary<string, object>>,
                Dictionary<string, object>>(bucket =>
                {
                    return MergeBucket(bucket);
                });

            Parser.LinkTo(Merger);
            Merger.LinkTo(bed);

            Parser.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)Merger).Fault(t.Exception);
                else Merger.Complete();
            });

            Merger.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)bed).Fault(t.Exception);
                else bed.Complete();
            });
        }

        private Dictionary<string, object> MergeBucket(IEnumerable<Dictionary<string, object>> bucket)
        {
            var mergedDrop = new Dictionary<string, object>();

            if (bucket.Count() > 1)
            {
                foreach (var drop in bucket)
                {
                    Merge(drop, mergedDrop);
                }
            }
            else
            {
                mergedDrop = bucket.First();
            }

            return mergedDrop;
        }

        private IEnumerable<Dictionary<string, object>> ParseBucket(IEnumerable<Dictionary<string, object>> bucket)
        {
            var parsedBucket = new List<Dictionary<string, object>>();

            foreach (var drop in bucket)
            {
                var parsedDrop = new Dictionary<string, object>();

                foreach (var header in drop.Keys)
                {
                    ParseColumn(header, drop[header], parsedDrop);
                }

                parsedBucket.Add(parsedDrop);
            }

            return parsedBucket;
        }

        public IEnumerable<IEnumerable<Dictionary<string, object>>> GetWater()
        {
            var bucket = new List<Dictionary<string, object>>();
            string current = null;

            foreach (var drop in GetDrops())
            {
                if (drop.ContainsKey("_id"))
                {
                    // Id found, check current
                    if (current == null || drop["_id"].ToString() == current)
                    {
                        // Do nothing special, noop
                    }
                    else
                    {
                        // Yield aggregated bucket of water.
                        yield return bucket;

                        // Reset bucket
                        bucket = new List<Dictionary<string, object>>();
                    }

                    bucket.Add(drop);
                    current = drop["_id"].ToString();
                }
                else
                {
                    // No _id, object cannot be merged therefore is it's own thing
                    yield return new List<Dictionary<string, object>>() { drop };
                }
            }

            // Yield the last bucket if there is one there
            if (bucket.Count > 0) yield return bucket;
        }

        private void Merge(Dictionary<string, object> src, Dictionary<string, object> dest)
        {
            foreach (var skvp in src)
            {
                if (!dest.ContainsKey(skvp.Key))
                {
                    dest.Add(skvp.Key, skvp.Value);
                }
                else if (skvp.Value.GetType() == typeof(Dictionary<string, object>))
                {
                    Merge(skvp.Value as Dictionary<string, object>, dest[skvp.Key] as Dictionary<string, object>);
                }
                else if (skvp.Value.GetType() == typeof(List<Dictionary<string, object>>))
                {
                    var srcList = skvp.Value as List<Dictionary<string, object>>;
                    var destList = dest[skvp.Key] as List<Dictionary<string, object>>;

                    foreach (var srcChild in srcList)
                    {
                        Dictionary<string, object> destMatch = null;
                        foreach (var destChild in destList)
                        {
                            if (destChild.ContainsKey("_id") && srcChild.ContainsKey("_id")
                                && destChild["_id"].ToString() == srcChild["_id"].ToString())
                            {
                                destMatch = destChild;
                                break;
                            }
                        }

                        if (destMatch != null) Merge(srcChild, destMatch);
                        else destList.Add(srcChild);
                    }
                }
                else if (skvp.Value.GetType() == typeof(List<object>))
                {
                    var srcList = skvp.Value as List<object>;
                    var destList = dest[skvp.Key] as List<object>;

                    foreach (var srcChild in srcList)
                    {
                        if (!destList.Contains(srcChild))
                            destList.Add(srcChild);
                    }
                }
                else
                {
                    dest[skvp.Key] = skvp.Value;
                }
            }
        }

        private void ParseColumn(string column, object data, Dictionary<string, object> parentObj)
        {
            // First child is property
            if ((column.IndexOf('.') > -1 && column.IndexOf('[') > -1 && column.IndexOf('.') < column.IndexOf('['))
                || (column.IndexOf('.') > -1 && column.IndexOf(']') == -1))
            {
                var idx = column.IndexOf('.');
                var name = column.Substring(0, idx);

                if (!parentObj.ContainsKey(name))
                    parentObj[name] = new Dictionary<string, object>();

                ParseColumn(column.Substring(idx + 1).Trim(), data, (parentObj[name] as Dictionary<string, object>));
            }
            // First child is array of primitives
            else if (column.IndexOf('[') > -1 && column.IndexOf(']') == column.IndexOf('[') + 1)
            {
                var idx = column.IndexOf('[');
                var name = column.Substring(0, idx);

                if (!parentObj.ContainsKey(name))
                    parentObj[name] = new List<object>() { data };
                else
                {
                    var list = parentObj[name] as List<object>;
                    if (!list.Contains(data)) list.Add(data);
                }
            }
            // First child is array of objects
            else if ((column.IndexOf('[') > -1 && column.IndexOf('.') > -1 && column.IndexOf('[') < column.IndexOf('.'))
                || (column.IndexOf('[') > -1 && column.IndexOf('.') == -1))
            {
                var idx = column.IndexOf('[');
                var name = column.Substring(0, idx);

                var childName = column.Substring(idx + 1);

                if ((childName.IndexOf(']') > -1 && childName.IndexOf('[') > -1 && childName.IndexOf(']') < childName.IndexOf('['))
                    || (childName.IndexOf(']') > -1 && childName.IndexOf('[') == -1))
                {
                    var remove = childName.IndexOf(']');
                    childName = childName.Substring(0, remove) + childName.Substring(remove + 1, childName.Length - remove - 1);
                }

                if (!parentObj.ContainsKey(name))
                    parentObj[name] = new List<Dictionary<string, object>>() { new Dictionary<string, object>() };

                ParseColumn(childName, data, (parentObj[name] as List<Dictionary<string, object>>)[0] as Dictionary<string, object>);

            }
            // No children
            else
            {
                parentObj[column] = data;
            }
        }

        internal void Pull(IEnumerable<Dictionary<string, object>> bucket)
        {
            Parser.Post(bucket);
        }

        internal void Empty()
        {
            Parser.Complete();
        }
    }
}
