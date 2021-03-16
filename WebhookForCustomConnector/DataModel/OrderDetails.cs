using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebHookForCustomConnector.DataModel
{
    public class OrderDetails
    {
        public OrderDetails()
        {
            Orders = new List<Order>();
        }
        public List<Order> Orders { get; set; }
    }
    public class Order
    {
        public string CustomerName { get; set; }
        public string OrderNumber { get; set; }
        public double Total { get; set; }
    }
}
