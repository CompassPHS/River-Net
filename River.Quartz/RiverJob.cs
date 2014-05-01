using Quartz;
using River.Components;
using River.Components.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Quartz
{
    public class RiverJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            var key = context.JobDetail.Key;

            var dataMap = context.JobDetail.JobDataMap;
            var riverContext = (RiverContext)dataMap["riverContext"];

            var river = new Components.River(riverContext);
            river.Flow();
        }
    } 
}
