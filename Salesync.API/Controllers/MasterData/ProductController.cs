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

        public ProductController(
            IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products =
                await _productService.GetAllAsync();

            return Ok(
                ApiResponse<IEnumerable<ProductDto>>
                    .SuccessResponse(
                        products,
                        "Products retrieved successfully",
                        200));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product =
                await _productService.GetByIdAsync(id);

            return Ok(
                ApiResponse<ProductDto>
                    .SuccessResponse(
                        product,
                        "Product retrieved successfully",
                        200));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductDto createProductDto)
        {
            var createdProduct =
                await _productService.CreateAsync(
                    createProductDto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdProduct.Id },
                ApiResponse<ProductDto>.SuccessResponse(
                    createdProduct,
                    "Product created successfully",
                    201));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateProductDto updateProductDto)
        {
            if (id != updateProductDto.Id)
            {
                return BadRequest(
                    ApiResponse<object>.ErrorResponse(
                        "ID in URL does not match ID in body.",
                        400));
            }

            var updatedProduct =
                await _productService.UpdateAsync(
                    id,
                    updateProductDto);

            return Ok(
                ApiResponse<ProductDto>.SuccessResponse(
                    updatedProduct,
                    "Product updated successfully.",
                    200));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteAsync(id);

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    null,
                    $"Product with ID {id} deactivated successfully."));
        }
    }
}