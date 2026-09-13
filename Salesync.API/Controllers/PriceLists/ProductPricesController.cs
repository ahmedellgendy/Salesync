using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Modules.MasterData.PriceLists.Dtos;
using Salesync.Application.Modules.MasterData.PriceLists.Interfaces;

namespace Salesync.API.Controllers.PriceLists;

[ApiController]
[Route("api/product-prices")]
[Authorize(Roles = "Admin")]
public class ProductPricesController : ControllerBase
{
    private readonly IProductPriceService _service;

    public ProductPricesController(
        IProductPriceService service)
    {
        _service = service;
    }

    [HttpGet]   // GET: api/product-prices
    public async Task<IActionResult> GetAll(
        [FromQuery] int? priceListId,
        [FromQuery] int? productId)
    {
        var result =
            await _service.GetAllAsync(
                priceListId,
                productId);

        return Ok(result);
    }

    [HttpGet("{id:int}")] // GET: api/product-prices/{id}
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _service.GetByIdAsync(id);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]  // POST: api/product-prices
    public async Task<IActionResult> Create(
        [FromBody] CreateProductPriceDto dto)
    {
        var result =
            await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]   // PUT: api/product-prices/{id}
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductPriceDto dto)
    {
        var result =
            await _service.UpdateAsync(id, dto);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]    // DELETE: api/product-prices/{id}
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);

        return NoContent();
    }
}