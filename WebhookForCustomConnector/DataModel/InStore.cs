using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebhookForCustomConnector.DataModel
{
    public class InStore
    {
        public string StoreName { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
}
