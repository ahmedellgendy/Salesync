using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Dtos.WarehouseDto;
using Salesync.Application.Interfaces.Services;

namespace Salesync.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;

        }


        [HttpGet]  // GET: api/Warehouse --> Get All Warehouses
        public async Task<IActionResult> GetAllAsync()
        {
            var wareHouses = await _warehouseService.GetAllAsync();
            return Ok(wareHouses);
        }


        [HttpGet("{id}")] // GET: api/Warehouse/id --> Get Warehouse By Id 
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var wareHouse = await _warehouseService.GetByIdAsync(id);
            if (wareHouse == null)
                return NotFound();

            return Ok(wareHouse);

        }


        [HttpPost]  // POST: api/Warehouse --> Create New Warehouse
        public async Task<IActionResult> CreateAsync([FromBody] CreateWarehouseDto warehouseDto)
        {
            await _warehouseService.CreateAsync(warehouseDto);
            return Ok(warehouseDto);
        }

        [HttpPut("{id}")]  // PUT: api/Warehouse/id --> Update Warehouse
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateWarehouseDto warehouseDto)
        {
            var updatedWarehouse = await _warehouseService.UpdateAsync(id,warehouseDto);
            return Ok(new
            {
                Message = $"Warehouse with id {id} updated successfully",
                UpdatedWarehouse = updatedWarehouse
            });
        } 

        [HttpDelete("{id}")] // DELETE: api/Warhouse --> Delete Warehouse 
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var warehouse = await _warehouseService.DeleteAsync(id);
            
            if(!warehouse)
                return NotFound($"Warehouse with id {id} not found");

            return NoContent();

        }

    }
}
