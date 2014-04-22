using River.Components;
using River.Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace River.Api.Controllers
{
    public class RiverController : ApiController
    {
        // GET api/river
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/river/5
        public RiverContext Get(string name)
        {
            return new RiverContext() { Name = name };
        }

        /*
                // POST api/river
                public void Post([FromBody]string value)
                {
                }
        */
        // PUT api/river/5
        public void Put(string id, RiverContext riverContext)
        {
            WebApiApplication.SchedulerWrapper.ScheduleJob(riverContext);
        }

        // DELETE api/river/5
        public void Delete(string id)
        {
        }
    }
}
