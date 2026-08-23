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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var warehouses =
                await _warehouseService.GetAllAsync();

            return Ok(
                ApiResponse<IEnumerable<WarehouseDto>>
                    .SuccessResponse(
                        warehouses,
                        "Warehouses retrieved successfully",
                        200));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var warehouse =
                await _warehouseService.GetByIdAsync(id);

            if (warehouse == null)
            {
                return NotFound(
                    ApiResponse<object>.NotFoundResponse(
                        $"Warehouse with ID {id} not found"));
            }

            return Ok(
                ApiResponse<WarehouseDto>
                    .SuccessResponse(
                        warehouse,
                        "Warehouse retrieved successfully",
                        200));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateWarehouseDto warehouseDto)
        {
            var createdWarehouse =
                await _warehouseService.CreateAsync(warehouseDto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdWarehouse.Id },
                ApiResponse<WarehouseDto>.SuccessResponse(
                    createdWarehouse,
                    "Warehouse created successfully",
                    201));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateWarehouseDto warehouseDto)
        {
            var updatedWarehouse =
                await _warehouseService.UpdateAsync(
                    id,
                    warehouseDto);

            return Ok(
                ApiResponse<WarehouseDto>
                    .SuccessResponse(
                        updatedWarehouse,
                        $"Warehouse with ID {id} updated successfully",
                        200));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _warehouseService.DeleteAsync(id);

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    null,
                    $"Warehouse with ID {id} deactivated successfully"));
        }
    }
}