using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebhookForCustomConnector.DataModel
{
    public enum TypeEvent
    {
        NewOrder=1,
        InStore=2
    }
    
    public class Subscription
    {
        public TypeEvent Event { get; set; }
        public string Id { get; set; }
        public string CallBackUrl { get; set; }
        public string CreatedTime { get; set; }
        public string LastStartTime { get; set; }
        public string Upn { get; set; }
        public string Oid { get; set; }
        public string Name { get; set; }
    }
    public class CallBack
    {
        public string Url { get; set; }
    }

}
