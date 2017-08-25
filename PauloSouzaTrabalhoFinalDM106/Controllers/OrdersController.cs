using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using PauloSouzaTrabalhoFinalDM106.Models;
using PauloSouzaTrabalhoFinalDM106.CRMClient;
using PauloSouzaTrabalhoFinalDM106.br.com.correios.ws;
using System.Globalization;

namespace PauloSouzaTrabalhoFinalDM106.Controllers
{
    [Authorize(Roles = "ADMIN, USER")]
    [RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        private PauloSouzaTrabalhoFinalDM106Context db = new PauloSouzaTrabalhoFinalDM106Context();

        // GET: api/Orders
        [Authorize(Roles = "ADMIN")]
        public List<Order> GetOrders()
        {
            return db.Orders.Include(order => order.OrderItems).ToList();
        }

        // GET: api/Orders/5
        [ResponseType(typeof(Order))]
        public IHttpActionResult GetOrder(int id)
        {
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return BadRequest("Error. The order cannot be found.");
            }
            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.email))
            {
                return BadRequest("User not Authorized.");
            }
            return Ok(order);
        }

        // GET: api/Orders/byemail
        [ResponseType(typeof(IQueryable<Order>))]
        [HttpGet]
        [Route("byemail")]
        public IHttpActionResult GetOrderByEmail(string email)
        {
            var orders = db.Orders.Where(o => o.email == email);
            if (orders.Count() == 0)
            {
                return BadRequest("The order cannot be found.");
            }
            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(email))
            {
                return BadRequest("User not Authorized.");
            }
            return Ok(orders.ToList());
        }

        // PUT: api/Orders/closeorder?id=2
        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("closeorder")]
        public IHttpActionResult CloseOrder(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null)
            {
                return BadRequest("The order cannot be found.");
            }

            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.email))
            {
                return BadRequest("User not Authorized.");
            }

            if (order.shippingPrice == null || order.shippingPrice == 0)
            {
                return BadRequest("The shipping price was not calculated.");
            }

            order.status = "FECHADO";

            db.Entry(order).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok("The order was set to 'FECHADO'.");
        }

        // PUT: api/Orders/5
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutOrder(int id, Order order)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (id != order.Id)
            {
                return BadRequest("The order number " + id + " doesn't match.");
            }
            var lookForOrder = db.Orders.Find(id);
            if (lookForOrder == null)
            {
                return BadRequest("The order cannot be found.");
            }
            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.email))
            {
                return BadRequest("User not Authorized.");
            }

            db.Entry(order).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Orders
        [ResponseType(typeof(Order))]
        public IHttpActionResult PostOrder(Order order)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (order.email == null || order.email.Equals(""))
            {
                order.email = User.Identity.Name;
            }
            if (!order.email.Equals(User.Identity.Name))
            {
                return BadRequest("Error. The email provided is not yours.");
            }
            DateTime date = DateTime.Now;
            order.orderDate = date;
            order.deliveryDate = date;
            if (order.status == null || order.status.Equals(""))
            {
                order.status = "NOVO";
            }
            order.status = order.status.ToUpper();
            order.email = User.Identity.Name;
            if (order.status.Equals("NOVO") || order.status.Equals("FECHADO") ||
                order.status.Equals("CANCELADO") || order.status.Equals("ENTREGUE"))
            {
                db.Orders.Add(order);
                db.SaveChanges();

                return CreatedAtRoute("DefaultApi", new { id = order.Id }, order);
            }
            else
            {
                return BadRequest("The order status: " + order.status + " is invalid.");
            }

        }

        // DELETE: api/Orders/5
        [ResponseType(typeof(Order))]
        public IHttpActionResult DeleteOrder(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null)
            {
                return BadRequest("The order cannot be found.");
            }
            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.email))
            {
                return BadRequest("User not Authorized.");
            }

            db.Orders.Remove(order);
            db.SaveChanges();

            return Ok(order);
        }

        // GET: api/orders/calcshipping?id=2
        [ResponseType(typeof(void))]
        [HttpGet]
        [Route("calcshipping")]
        public IHttpActionResult CalcShipping(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null)
            {
                return BadRequest("The order cannot be found.");
            }
            if (!order.status.Equals("NOVO") && !order.status.Equals("novo"))
            {
                return BadRequest("The status of this order is not 'NOVO'.");
            }
            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.email))
            {
                return BadRequest("User not Authorized.");
            }
            if (order.OrderItems == null || order.OrderItems.Count == 0)
            {
                return BadRequest("This order doesn't have any items.");
            }
            Customer customer;
            try
            {
                CRMRestClient crmClient = new CRMRestClient();

                customer = crmClient.GetCustomerByEmail(order.email);
            }
            catch (Exception e)
            {
                return BadRequest("It is not possible to access the CRM service.");
            }
            cResultado resultado;
            try
            {
                decimal totalWeight = 0;
                decimal totalWidth = 0;
                decimal totalHeight = 0;
                decimal totalDiameter = 0;
                decimal totalLength = 0;
                decimal totalOrder = 0;
                foreach (OrderItem orderItem in order.OrderItems)
                {
                    var product = db.Products.Find(orderItem.ProductId);
                    totalHeight = product.height;
                    totalDiameter = product.diameter;
                    totalLength = product.length;
                    for (int i = 0; i < orderItem.quantity; i++)
                    {
                        totalWeight += product.weight;
                        totalWidth += product.width;
                        totalOrder += product.price;
                    }
                }
                CalcPrecoPrazoWS correios = new CalcPrecoPrazoWS();
                try
                {
                    resultado = correios.CalcPrecoPrazo("", "", "40010", "37540000",
                            customer.zip, totalWeight.ToString(), 1, totalLength, totalHeight, totalWidth,
                            totalDiameter, "N", totalOrder, "S");
                }
                catch (Exception e)
                {
                    return BadRequest("It is not possible to access the CORREIOS service.");
                }

                if (!resultado.Servicos[0].Erro.Equals("0"))
                {
                    return BadRequest("Correios response error. Error code: " + resultado.Servicos[0].Codigo + " | Message: " + resultado.Servicos[0].MsgErro);
                }
                try
                {
                    NumberFormatInfo nfi = new CultureInfo("pt-BR", false).NumberFormat;
                    order.shippingPrice = decimal.Parse(resultado.Servicos[0].Valor, nfi);
                    order.orderTotalPrice = totalOrder;
                    order.orderTotalWeight = totalWeight;
                    DateTime dt = DateTime.Today.AddDays(Convert.ToInt16(resultado.Servicos[0].PrazoEntrega));
                    order.deliveryDate = dt;
                }
                catch (Exception e2)
                {
                    return BadRequest("The order was not updated.");
                }
            }
            catch (Exception e)
            {
                return BadRequest("It is not possible to access the CORREIOS service.");
            }
            db.Entry(order).State = EntityState.Modified;
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return BadRequest("This order doesn't exist.");
                }

            }
            return Ok(resultado);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool OrderExists(int id)
        {
            return db.Orders.Count(e => e.Id == id) > 0;
        }
    }
}