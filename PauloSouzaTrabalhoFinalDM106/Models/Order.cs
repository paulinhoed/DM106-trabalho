using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PauloSouzaTrabalhoFinalDM106.Models
{
    public class Order
    {
        public Order()
        {
            this.OrderItems = new HashSet<OrderItem>();
        }

        public int Id { get; set; }

        public string email { get; set; }

        public DateTime orderDate { get; set; }

        public DateTime deliveryDate { get; set; }

        public string status { get; set; }

        public decimal orderTotalPrice { get; set; }

        public decimal orderTotalWeight { get; set; }

        public decimal shippingPrice { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}