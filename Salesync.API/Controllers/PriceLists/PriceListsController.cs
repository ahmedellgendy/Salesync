using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Modules.MasterData.PriceLists.Dtos;
using Salesync.Application.Modules.MasterData.PriceLists.Interfaces;

namespace Salesync.API.Controllers.PriceLists;

[ApiController]
[Route("api/price-lists")]
[Authorize(Roles = "Admin")]
public class PriceListsController : ControllerBase
{
    private readonly IPriceListService _priceListService;

    public PriceListsController(
        IPriceListService priceListService)
    {
        _priceListService = priceListService;
    }

    [HttpGet]   // GET: api/price-lists
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _priceListService.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]   // GET: api/price-lists/{id}
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _priceListService.GetByIdAsync(id);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]  // POST: api/price-lists
    public async Task<IActionResult> Create(
        [FromBody] CreatePriceListDto dto)
    {
        var result =
            await _priceListService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]   // PUT: api/price-lists/{id}
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdatePriceListDto dto)
    {
        var result =
            await _priceListService.UpdateAsync(
                id,
                dto);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]    // DELETE: api/price-lists/{id}
    public async Task<IActionResult> Delete(int id)
    {
        await _priceListService.DeleteAsync(id);

        return NoContent();
    }
}