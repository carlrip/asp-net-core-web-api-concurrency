using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public ProductsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IEnumerable<Product>> GetAll()
        {
            using (var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]))
            {
                await connection.OpenAsync();
                var products = await connection.QueryAsync<Product>("SELECT * FROM Product");

                return products;
            }
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<Product>> GetById(Guid productId)
        {
            using (var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]))
            {
                await connection.OpenAsync();
                var product = await connection.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Product WHERE ProductId = @ProductId", new { ProductId = productId });
                if (product == null) return NotFound();
                return Ok(product);
            }
        }

        [HttpPut()]
        public async Task<ActionResult<Product>> Put([FromBody] Product product)
        {
            using (var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]))
            {
                await connection.OpenAsync();
                var existingProduct = await connection.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Product WHERE ProductId = @ProductId", new { ProductId = product.ProductId });
                if (existingProduct == null)
                {
                    return new NotFoundResult();
                }
                if (Convert.ToBase64String(existingProduct.Version) != Convert.ToBase64String(product.Version))
                {
                    return StatusCode(409);
                }
                var rowsUpdated = await connection.ExecuteAsync(@"UPDATE Product
                                                                SET ProductName=@ProductName,
                                                                    UnitPrice=@UnitPrice,
                                                                    UnitsInStock=@UnitsInStock
                                                                WHERE ProductId = @ProductId
                                                                    AND Version = @Version",
                                                                product);
                if (rowsUpdated != 1)
                {
                    return StatusCode(409);
                }
                var savedProduct = await connection.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Product WHERE ProductId = @ProductId", new { ProductId = product.ProductId });
                return Ok(savedProduct);
            }
        }
    }
}
