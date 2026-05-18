using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.MasterData.Dtos.ProductDto;
using Salesync.Application.Modules.MasterData.Interfaces.Services;

namespace Salesync.API.Controllers.MasterData
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]  // GET: api/product
        public async Task<IActionResult> GetAllAsync()
        {
            var products = await _productService.GetAllAsync();

            return Ok(ApiResponse<IEnumerable<ProductDto>>.SuccessResponse(
                      products,
                      "products retrieved successfully",
                      200
                      ));
        }

        [HttpGet("{id}")]  // GET: api/product/id
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound(ApiResponse<object>.NotFoundResponse($"Product with ID {id} not found"));

            return Ok(ApiResponse<ProductDto>.SuccessResponse(
                      product,
                      "product retrieved successfully",
                      200
                      ));
        }

        [HttpPost]  // POST: api/product --> Create New Product
        public async Task<IActionResult> CreateAsync([FromBody] CreateProductDto createProductDto)
        {
            var createdProduct = await _productService.CreateAsync(createProductDto);
            return Ok(ApiResponse<ProductDto>.SuccessResponse(createdProduct, "Product created successfully", 201));

        }

        [HttpPut("{id:int}")] // PUT: api/product/id --> Update Product
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            if (id != updateProductDto.Id)
                return BadRequest(ApiResponse<object>.ErrorResponse("ID in URL does not match ID in body", 400));

            var updatedProduct = await _productService.UpdateAsync(id, updateProductDto);
            return Ok(ApiResponse<ProductDto>.SuccessResponse(
                updatedProduct,
                "product updated successfully."
                ));
        }
        [HttpDelete("{id}")]  //DELETE: api/product/id --> Delete Product
        public async Task<IActionResult> DeleteAsync(int id)
        {
            await _productService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(
                      null,
                      $"Product with ID {id} deleted successfully"
                      ));
        }
    }
}
