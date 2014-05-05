using Common.Logging;
using Newtonsoft.Json;
using River.Components.Contexts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace River.Components
{
    public class River
    {
        private RiverContext _riverContext;
        Sources.Source _source;
        Mouth _mouth;

        /// <summary>
        /// Links the Source of the River and the Mouth of the River.
        /// </summary>
        TransformBlock<
            Dictionary<string, object>,
            Dictionary<string, object>> Bed { get; set; }

        ILog log = Common.Logging.LogManager.GetCurrentClassLogger();

        public River(RiverContext riverContext)
        {
            _riverContext = riverContext;
            CreateFlow();

            _source = Sources.Source.GetSource(riverContext.Source, Bed);
            _mouth = new Mouth(riverContext.Destination, Bed);
        }

        private void CreateFlow()
        {
            Bed = new TransformBlock<Dictionary<string, object>, Dictionary<string, object>>(bucket =>
            {
                return bucket;
            });
        }

        public void Flow()
        {
            log.Info(string.Format("Starting river {0}", _riverContext.Name));

            try
            {
                var start = System.Environment.TickCount;
                foreach (var bucket in _source.GetWater())
                {
                    _source.Pull(bucket);
                }

                _source.Empty();
                _mouth.IsEmptied();
                var end = System.Environment.TickCount;
                log.Info(string.Format("River took {0}", (end - start) / 1000));
            }
            catch (Exception e)
            {
                log.Error(string.Format("Error river {0}", _riverContext.Name), e);
            }

            log.Info(string.Format("Completed river {0}", _riverContext.Name));
            
        }
    }
}
