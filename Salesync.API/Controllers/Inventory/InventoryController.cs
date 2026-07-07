using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Inventory.Dtos;
using Salesync.Application.Modules.Inventory.Interfaces;

namespace Salesync.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPost("opening-balance")]  // POST: api/inventory/opening-balance
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> CreateOpeningBalance(CreateOpeningBalanceDto dto)
        {
            var result = await _inventoryService.CreateOpeningBalanceAsync(dto);
            return Ok(ApiResponse<StockBalanceDto>.SuccessResponse(result, "Opening balance created successfully"));
        }


        [HttpGet("balances")]   // GET: api/inventory/balances
        public async Task<IActionResult> GetAllBalances()
        {
            var result = await _inventoryService.GetAllBalancesAsync();
            return Ok(ApiResponse<IEnumerable<StockBalanceDto>>.SuccessResponse(result));
        }


        [HttpGet("balances/{productId:int}/{warehouseId:int}")] // GET: api/inventory/balances/{productId}/{warehouseId}
        public async Task<IActionResult> GetBalance(int productId, int warehouseId)
        {
            var result = await _inventoryService.GetBalanceAsync(productId, warehouseId);
            return Ok(ApiResponse<StockBalanceDto>.SuccessResponse(result));
        }


        [HttpGet("movements")]  // GET: api/inventory/movements?productId={productId}&warehouseId={warehouseId}
        public async Task<IActionResult> GetMovements([FromQuery] int? productId, [FromQuery] int? warehouseId)
        {
            var result = await _inventoryService.GetMovementsAsync(productId, warehouseId);
            return Ok(ApiResponse<IEnumerable<StockMovementDto>>.SuccessResponse(result));
        }


        [HttpPost("stock-in")]  // POST: api/inventory/stock-in
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> StockIn(CreateStockInDto dto)
        {
            var result = await _inventoryService.StockInAsync(
                dto.ProductId,
                dto.WarehouseId,
                dto.Quantity,
                notes: dto.Notes);
            return Ok(ApiResponse<StockMovementDto>.SuccessResponse( result, "Stock in created successfully"));
        }


        [HttpPost("stock-out")] // POST: api/inventory/stock-out
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> StockOut(CreateStockOutDto dto)
        {
            var result = await _inventoryService.StockOutAsync(
                dto.ProductId,
                dto.WarehouseId,
                dto.Quantity,
                notes: dto.Notes);
            return Ok(ApiResponse<StockMovementDto>.SuccessResponse( result, "Stock out created successfully"));
        }

    }
}
