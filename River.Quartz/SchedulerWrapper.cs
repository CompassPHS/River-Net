using Quartz;
using Quartz.Impl;
using Quartz.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Quartz
{
    public class SchedulerWrapper
    {
        private SchedulerWrapper()
        {

        }

        private static SchedulerWrapper _schedulerWrapper;

        public static SchedulerWrapper GetSchedulerWrapper()
        {
            if (_schedulerWrapper == null) _schedulerWrapper = new SchedulerWrapper();

            return _schedulerWrapper;
        }

        private IScheduler _scheduler;

        public void Load()
        {
            try
            {
                ISchedulerFactory sf = new StdSchedulerFactory();
                _scheduler = sf.GetScheduler();

                _scheduler.Start();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Unload()
        {
            _scheduler.Shutdown();
        }

        public void ScheduleJob(River.Components.RiverContext riverContext)
        {
            IJobDetail job = JobBuilder.Create<RiverJob>()
                .WithIdentity(riverContext.Name) // name "myRiver", group "river"
                //                .SetJobData(jobData)
                .UsingJobData("name", riverContext.Name)
                .UsingJobData("cron", riverContext.Cron)
                .UsingJobData("maxBulkSize", riverContext.MaxBulkSize)
                .UsingJobData("suppressNulls", riverContext.SuppressNulls)

                .UsingJobData("source.server", riverContext.Source.Server)
                .UsingJobData("source.database", riverContext.Source.Database)
                .UsingJobData("source.user", riverContext.Source.User)
                .UsingJobData("source.password", riverContext.Source.Password)
                .UsingJobData("source.sql.command", riverContext.Source.Sql.Command)
                .UsingJobData("source.sql.isProc", riverContext.Source.Sql.IsProc)

                .UsingJobData("destination.url", riverContext.Destination.Url)
                .UsingJobData("destination.index", riverContext.Destination.Index)
                .UsingJobData("destination.type", riverContext.Destination.Type)
                .Build();

            var trigger = (!String.IsNullOrWhiteSpace(riverContext.Cron))
            ? TriggerBuilder.Create()
                .WithIdentity(riverContext.Name) //, "group1")
                .WithCronSchedule(riverContext.Cron)
                .ForJob(riverContext.Name) //, "group1")
                .Build()
            : TriggerBuilder.Create()
                .WithIdentity(riverContext.Name) //, "group1")
                .StartNow()
                .ForJob(riverContext.Name) //, "group1")
                .Build();

            if (_scheduler.CheckExists(new JobKey(riverContext.Name)))
                _scheduler.DeleteJob(new JobKey(riverContext.Name));

            _scheduler.ScheduleJob(job, trigger);
        }
    }
}
