using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Treasury.Dtos;
using Salesync.Application.Modules.Treasury.Interfaces.Services;

namespace Salesync.API.Controllers.Treasury
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TreasuryController : Controller
    {
        private readonly ITreasuryService _treasuryService;

        public TreasuryController(ITreasuryService treasuryService)
        {
            _treasuryService = treasuryService;
        }

        [HttpGet("cashboxes")] // GET: api/treasury/cashboxes
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> GetCashBoxes()
        {
            var result = await _treasuryService.GetCashBoxesAsync();
            return Ok(ApiResponse<IEnumerable<CashBoxDto>>.SuccessResponse(result, message: "Cash boxes retrieved successfully."));
        }

        [HttpGet("cashboxes/{id}")] // GET: api/treasury/cashboxes/{id}
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> GetCashBoxById(int id)
        {
            var result = await _treasuryService.GetCashBoxByIdAsync(id);
            return Ok(ApiResponse<CashBoxDto>.SuccessResponse(result, message: "Cash box retrieved successfully."));
        }

        [HttpPost("cashboxes")] // POST: api/treasury/cashboxes
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCashBox([FromBody] CreateCashBoxDto dto)
        {
            var result = await _treasuryService.CreateCashBoxAsync(dto);
            return Ok(ApiResponse<CashBoxDto>.SuccessResponse(result, "Cash box created successfully."));
        }

        [HttpPut("day-closing/{dayClosingId}/receive-cash")] // PUT: api/treasury/day-closing/{dayClosingId}/receive-cash
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> ReceiveDayClosingCash(int dayClosingId, [FromBody] ReceiveDayClosingCashDto dto)
        {
            var result = await _treasuryService.ReceiveDayClosingCashAsync(dayClosingId, dto);
            return Ok(ApiResponse<CashReceiptDto>.SuccessResponse(result, "Cash received successfully."));
        }

        [HttpGet("salesrep/{salesRepId}/ledger")] // GET: api/treasury/salesrep/{salesRepId}/ledger
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> GetSalesRepLedger(int salesRepId)
        {
            var result = await _treasuryService.GetSalesRepLedgerAsync(salesRepId);
            return Ok(ApiResponse<IEnumerable<SalesRepCashLedgerDto>>.SuccessResponse(result, "Sales rep cash ledger retrieved successfully."));
        }

        [HttpGet("salesrep/{salesRepId}/balance")] // GET: api/treasury/salesrep/{salesRepId}/balance
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> GetSalesRepCashBalance(int salesRepId)
        {
            var result = await _treasuryService.GetSalesRepCashBalanceAsync(salesRepId);
            return Ok(ApiResponse<decimal>.SuccessResponse(result, "Sales rep cash balance retrieved successfully."));
        }

        [HttpGet("transactions")] // GET: api/treasury/transactions?cashBoxId={cashBoxId}&fromDate={fromDate}&toDate={toDate}
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> GetTransactions(
                 [FromQuery] int? cashBoxId = null,
                 [FromQuery] DateTime? fromDate = null,
                 [FromQuery] DateTime? toDate = null)
        {
            var result =
                await _treasuryService
                    .GetTransactionsAsync(
                        cashBoxId,
                        fromDate,
                        toDate);

            return Ok(
                ApiResponse<IEnumerable<TreasuryTransactionDto>>
                    .SuccessResponse(
                        result,
                        "Treasury transactions retrieved successfully."));
        }

        [HttpPost("cash-out")]  // POST: api/treasury/cash-out
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> CreateCashOut([FromBody] CreateTreasuryCashOutDto dto)
        {
            var result =
                await _treasuryService
                    .CreateCashOutAsync(dto);

            return Ok(
                ApiResponse<TreasuryCashOutDto>
                    .SuccessResponse(
                        result,
                        "Cash out transaction created successfully."));
        }

        [HttpGet("expense-categories")] // GET: api/treasury/expense-categories
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> GetExpenseCategories()
        {
            var result =
                await _treasuryService
                    .GetExpenseCategoriesAsync();

            return Ok(
                ApiResponse<IEnumerable<ExpenseCategoryDto>>
                    .SuccessResponse(
                        result,
                        "Expense categories retrieved successfully."));
        }

        [HttpGet("expense-categories/{id}")] // GET: api/treasury/expense-categories/{id}
        [Authorize(Roles = "Admin,Treasury")]
        public async Task<IActionResult> GetExpenseCategoryById(int id)
        {
            var result =
                await _treasuryService
                    .GetExpenseCategoryByIdAsync(id);

            return Ok(
                ApiResponse<ExpenseCategoryDto>
                    .SuccessResponse(
                        result,
                        "Expense category retrieved successfully."));
        }

        [HttpPost("expense-categories")] // POST: api/treasury/expense-categories
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateExpenseCategory([FromBody] CreateExpenseCategoryDto dto)
        {
            var result =
                await _treasuryService
                    .CreateExpenseCategoryAsync(dto);

            return Ok(
                ApiResponse<ExpenseCategoryDto>
                    .SuccessResponse(
                        result,
                        "Expense category created successfully."));
        }

        [HttpPut("expense-categories/{id}")] // PUT: api/treasury/expense-categories/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateExpenseCategory(int id, [FromBody] UpdateExpenseCategoryDto dto)
        {
            var result =
                await _treasuryService
                    .UpdateExpenseCategoryAsync(
                        id,
                        dto);

            return Ok(
                ApiResponse<ExpenseCategoryDto>
                    .SuccessResponse(
                        result,
                        "Expense category updated successfully."));
        }
    }
}
