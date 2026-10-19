


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
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
        {
            var result = await _productService.CreateAsync(request);
            return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponse>> GetProductById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductResponse>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
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
        public async Task<ActionResult<ProductResponse>> DecrementStock(int id, int quantity)
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
            catch (ArgumentOutOfRangeException)
            {
                return Problem(
                    title: "Invalid quantity",
                    detail: "Quantity must be greater than zero.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            catch (InsufficientStockException ex)
            {
                return Problem(
                    title: "Insufficient stock",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        [HttpPost("{id}/add-to-stock/{quantity}")]
        public async Task<ActionResult<ProductResponse>> AddToStock(int id, int quantity)
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
            catch (ArgumentOutOfRangeException)
            {
                return Problem(
                    title: "Invalid quantity",
                    detail: "Quantity must be greater than zero.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            catch (StockLimitExceededException ex)
            {
                return Problem(
                    title: "Stock limit exceeded",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> SearchByName([FromQuery] SearchProductsRequest request)
        {
            var products = await _productService.GetByNameAsync(request.Name);
            return Ok(products);
        }

        [HttpGet("stock-level")]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetStockLevel([FromQuery] StockLevelRequest request)
        {
            var products = await _productService.GetWithStockLevelAsync(request.Min, request.Max);
            return Ok(products);
        }
    }
}
