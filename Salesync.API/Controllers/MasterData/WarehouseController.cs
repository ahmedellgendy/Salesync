using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.MasterData.Dtos.WarehouseDto;
using Salesync.Application.Modules.MasterData.Interfaces.Services;

namespace Salesync.API.Controllers.MasterData
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
            var warehouses = await _warehouseService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<WarehouseDto>>.SuccessResponse(
                warehouses,
                "Warehouses retrieved successfully",
                200
            ));
        }

        [HttpGet("{id}")] // GET: api/Warehouse/id --> Get Warehouse By Id 
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var warehouse = await _warehouseService.GetByIdAsync(id);
            if (warehouse == null)
                return NotFound(ApiResponse<object>.NotFoundResponse($"Warehouse with ID {id} not found"));

            return Ok(ApiResponse<WarehouseDto>.SuccessResponse(
                warehouse,
                "Warehouse retrieved successfully",
                200
            ));

        }

        [HttpPost]  // POST: api/Warehouse --> Create New Warehouse
        public async Task<IActionResult> CreateAsync([FromBody] CreateWarehouseDto warehouseDto)
        {
            var createdWarehouse = await _warehouseService.CreateAsync(warehouseDto);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = createdWarehouse.Id },
                   ApiResponse<WarehouseDto>.SuccessResponse(
                        createdWarehouse,
                        "Warehouse created successfully",
                        201
                        ));
        }

        [HttpPut("{id}")]  // PUT: api/Warehouse/id --> Update Warehouse
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateWarehouseDto warehouseDto)
        {
            var updatedWarehouse = await _warehouseService.UpdateAsync(id, warehouseDto);
            return Ok(ApiResponse<WarehouseDto>.SuccessResponse(
                updatedWarehouse,
                $"Warehouse with ID {id} updated successfully",
                200
            ));
        }

        [HttpDelete("{id}")] // DELETE: api/Warhouse --> Delete Warehouse 
        public async Task<IActionResult> Delete(int id)
        {
            var deletedWarehouse = await _warehouseService.DeleteAsync(id);
            if (!deletedWarehouse)
                return NotFound(ApiResponse<object>.NotFoundResponse($"Warehouse with ID {id} not found"));

            return Ok(ApiResponse<object>.SuccessResponse(
                      null,
                      $"Warehouse with ID {id} deleted successfully"
                      ));
        }

    }
}
