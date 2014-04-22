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

            //RiverContext riverContext = (RiverContext)dataMap["RiverContext"];

            var riverContext = new RiverContext()
            {
                Name = dataMap.GetString("name"),
                SuppressNulls = dataMap.GetBoolean("suppressNulls"),
                Cron = dataMap.GetString("cron"),

                Source = new Source()
                {
                    Server = dataMap.GetString("source.server"),
                    Database = dataMap.GetString("source.database"),
                    User = dataMap.GetString("source.user"),
                    Password = dataMap.GetString("source.password"),
                    Sql = new Sql()
                    {
                        Command = dataMap.GetString("source.sql.command"),
                        IsProc = dataMap.GetBoolean("source.sql.isProc")
                    }
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
