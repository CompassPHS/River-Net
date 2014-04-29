using Quartz;
using River.Components;
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

            var riverContext = new RiverContext()
            {
                Name = dataMap.GetString("name"),
                SuppressNulls = dataMap.GetBoolean("suppressNulls"),
                Cron = dataMap.GetString("cron"),

                Source = new Source()
                {
                    ConnectionString = dataMap.GetString("source.connectionString"),
                    Command = dataMap.GetString("source.command"),
                    CommandTimeout = dataMap.GetInt("source.commandTimeout")
                },

                Destination = new Destination()
                {
                    Url = dataMap.GetString("destination.url"),
                    Index = dataMap.GetString("destination.index"),
                    Type = dataMap.GetString("destination.type")
                }
            };

            var river = new Components.River(riverContext);
            river.Flow();
        }
    } 
}
