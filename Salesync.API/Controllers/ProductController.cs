using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Dtos.CustomerDto;
using Salesync.Application.Dtos.ProductDto;
using Salesync.Application.Interfaces.Services;

namespace Salesync.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IValidator<CreateProductDto> _createValidator;
        private readonly IValidator<UpdateProductDto> _updateValidator;

        public ProductController(IProductService productService, IValidator<CreateProductDto> createValidator, IValidator<UpdateProductDto> updateValidator)
        {
            _productService = productService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]  // GET: api/product
        public async Task<IActionResult> GetAllAsync()
        {
            var products = await _productService.GetAllAsync();
            if (products == null)
                return NoContent();

            return Ok(products);
        }

        [HttpGet("{id}")]  // GET: api/product/id
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var product = await _productService.GetByIdAsync(id);

            if (product == null)
                return NotFound($"product with id {id} not found");

            return Ok(product);

        }

        [HttpPost]  // POST: api/product --> Create New Product
        public async Task<IActionResult> CreateAsync([FromBody] CreateProductDto createProductDto)
        {
            // FluentValidation - Validate data format
            var validationResult = await _createValidator.ValidateAsync(createProductDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Validation failed",
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage)
                });
            }

            var createdProduct = await _productService.CreateAsync(createProductDto);

            return Ok(createdProduct);
        }

        [HttpPut("{id:int}")] // PUT: api/product/id --> Update Product
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            // FluentValidation 
            var validationResult = await _updateValidator.ValidateAsync(updateProductDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Validation failed",
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage)
                });
            }

            var updatedProduct = await _productService.UpdateAsync(id, updateProductDto);

            return Ok(new
            {
                Message = "Product Updated Successfully..",
                Product = updatedProduct

            });
        }
        [HttpDelete("{id}")]  //DELETE: api/product/id --> Delete Product
        public async Task<IActionResult> DeleteAsync(int id)
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
    }
}
