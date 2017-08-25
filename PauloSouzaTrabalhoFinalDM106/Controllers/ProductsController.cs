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

namespace PauloSouzaTrabalhoFinalDM106.Controllers
{
    [Authorize(Roles = "ADMIN, USER")]
    public class ProductsController : ApiController
    {
        private PauloSouzaTrabalhoFinalDM106Context db = new PauloSouzaTrabalhoFinalDM106Context();

        // GET: api/Products
        public IQueryable<Product> GetProducts()
        {
            return db.Products;
        }

        // GET: api/Products/5
        [ResponseType(typeof(Product))]
        public IHttpActionResult GetProduct(int id)
        {
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        // PUT: api/Products/5
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutProduct(int id, Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != product.Id)
            {
                return BadRequest("The id doesn't match.");
            }
            var productModel = db.Products.Where(p => p.model.ToUpper() == product.model.ToUpper() && p.Id != id);
            if (productModel.Count() != 0)
            {
                return BadRequest("Error. The model is already taken.");
            }
            var productCode = db.Products.Where(p => p.code.ToUpper() == product.code.ToUpper() && p.Id != id);
            if (productCode.Count() != 0)
            {
                return BadRequest("Error. The code is already taken.");
            }

            db.Entry(product).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return CreatedAtRoute("DefaultApi", new { id = product.Id }, product);
        }

        // POST: api/Products
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(Product))]
        public IHttpActionResult PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var productModel = db.Products.Where(p => p.model.ToUpper() == product.model.ToUpper());
            if (productModel.Count() > 0)
            {
                return BadRequest("Error. The model " + product.model.ToUpper() + " already exists.");
            }

            var productCode = db.Products.Where(p => p.code.ToUpper() == product.code.ToUpper());
            if (productCode.Count() > 0)
            {
                return BadRequest("Error. The code " + product.code.ToUpper() + " already exists.");
            }

            db.Products.Add(product);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = product.Id }, product);
        }

        // DELETE: api/Products/5,
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(Product))]
        public IHttpActionResult DeleteProduct(int id)
        {
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            db.Products.Remove(product);
            db.SaveChanges();

            return Ok(product);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ProductExists(int id)
        {
            return db.Products.Count(e => e.Id == id) > 0;
        }
    }
}