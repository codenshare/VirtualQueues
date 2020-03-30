using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Routing;

namespace MultiDialogsBot.Controllers
{
   
    public class QueueController : ApiController
    {
        // GET: api/Queue
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Queue/5
        public string Get(int id)
        {
            return "value";
        }

        [HttpPost]
        // POST: api/Queue
        public void Post() //string storeid
        {
            QueueService qS = new QueueService();
           
            qS.MonitorInviteQueues();
            qS.MonitorInStoreQueues();

            //Call Monitor Queue & Invite Queue.


        }


        [HttpPost]
        // POST: api/Queue
        public bool Post(string secretcode, string storeid)
        {
            QueueService qS = new QueueService();
            var response = qS.CustomerEntersTheStore(secretcode, storeid);
            return response;
        }


        //[Route("[action]")]
        //// POST: api/Queue
        //public bool CustomerEntersTheStore(string CustomerID, string StoreID)
        //{

        //    return true;
        //}

        // PUT: api/Queue/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Queue/5
        public void Delete(int id)
        {
        }
    }
}
