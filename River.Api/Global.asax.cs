using River.Components.Contexts.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace River.Api
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        public static River.Quartz.SchedulerWrapper SchedulerWrapper;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            var formatters = GlobalConfiguration.Configuration.Formatters;
	        var jsonFormatter = formatters.JsonFormatter;
	        jsonFormatter.SerializerSettings.Converters.Add(new SourceConverter());

            SchedulerWrapper = River.Quartz.SchedulerWrapper.GetSchedulerWrapper();
            SchedulerWrapper.Load();
        }

        protected void Application_End()
        {
            SchedulerWrapper.Unload();
        }
    }
}