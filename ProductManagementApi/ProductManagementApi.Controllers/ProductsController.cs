


namespace ProductManagementApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using ProductManagementApi.DTO;
    using ProductManagementApi.Exceptions;
    using ProductManagementApi.Services;

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            var result = await _productService.CreateAsync(request);
            return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            var result = await _productService.UpdateByIdAsync(id, request);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }

            return NoContent();
        }


        [HttpPost("{id}/decrement-stock/{quantity}")]
        public async Task<IActionResult> DecrementStock(int id, int quantity)
        {
            try
            {
                var product = await _productService.DecrementStockAsync(id, quantity);
                if (product == null)
                {
                    return NotFound();
                }

                return Ok(product);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InsufficientStockException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/add-to-stock/{quantity}")]
        public async Task<IActionResult> AddToStock(int id, int quantity)
        {
            try 
            {
                var product = await _productService.IncrementStockAsync(id, quantity);
                if (product == null)
                {
                    return NotFound();
                }

                return Ok(product);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByName([FromQuery] SearchProductsRequest request)
        {
            var products = await _productService.GetByNameAsync(request.Name);
            return Ok(products);
        }

        [HttpGet("stock-level")]
        public async Task<IActionResult> GetStockLevel([FromQuery] StockLevelRequest request)
        {
            var products = await _productService.GetWithStockLevelAsync(request.Min, request.Max);
            return Ok(products);
        }
    }
}
