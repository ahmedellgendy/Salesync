using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.DataImport.Dtos;
using Salesync.Application.Modules.DataImport.Interfaces;
using System.Security.Claims;

namespace Salesync.API.Controllers.DataImport
{
    [Route("api/data-import")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DataImportController : ControllerBase
    {
        private readonly IBranchImportService _branchImportService;
        private readonly IWarehouseImportService _warehouseImportService;
        private readonly IProductImportService _productImportService;
        private readonly ICustomerImportService _customerImportService;

        public DataImportController(
            IBranchImportService branchImportService,
            IWarehouseImportService warehouseImportService,
            IProductImportService productImportService,
            ICustomerImportService customerImportService)
        {
            _branchImportService = branchImportService;
            _warehouseImportService = warehouseImportService;
            _productImportService = productImportService;
            _customerImportService = customerImportService;
        }

        // =====================================================
        // BRANCHES
        // =====================================================

        [HttpGet("branches/template")]
        public IActionResult DownloadBranchTemplate()
        {
            var file =
                _branchImportService.GenerateTemplate();

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Salesync-Branches-Template.xlsx");
        }

        [HttpPost("branches/validate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ValidateBranchesAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var validationError =
                ValidateExcelFile(file);

            if (validationError is not null)
                return validationError;

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            await using var stream =
                file.OpenReadStream();

            var result =
                await _branchImportService.ValidateAsync(
                    stream,
                    file.FileName,
                    userId,
                    cancellationToken);

            return Ok(
                ApiResponse<ImportPreviewDto>
                    .SuccessResponse(
                        result,
                        result.CanImport
                            ? "File validated successfully."
                            : "File contains validation errors."));
        }

        [HttpPost("branches/{batchId:int}/import")]
        public async Task<IActionResult> ImportBranchesAsync(
            int batchId,
            CancellationToken cancellationToken)
        {
            var result =
                await _branchImportService.ImportAsync(
                    batchId,
                    cancellationToken);

            return Ok(
                ApiResponse<ImportResultDto>
                    .SuccessResponse(
                        result,
                        result.Message));
        }

        [HttpGet("branches/{batchId:int}/errors")]
        public async Task<IActionResult> DownloadBranchErrorsAsync(
            int batchId,
            CancellationToken cancellationToken)
        {
            var file =
                await _branchImportService
                    .GenerateErrorReportAsync(
                        batchId,
                        cancellationToken);

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Salesync-Branch-Import-Errors-{batchId}.xlsx");
        }

        [HttpGet("branches/history")]
        public async Task<IActionResult> GetBranchImportHistoryAsync(
            CancellationToken cancellationToken)
        {
            var result =
                await _branchImportService.GetHistoryAsync(
                    cancellationToken);

            return Ok(
                ApiResponse<IEnumerable<ImportBatchHistoryDto>>
                    .SuccessResponse(result));
        }

        // =====================================================
        // WAREHOUSES
        // =====================================================

        [HttpGet("warehouses/template")]
        public IActionResult DownloadWarehouseTemplate()
        {
            var file =
                _warehouseImportService.GenerateTemplate();

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Salesync-Warehouses-Template.xlsx");
        }

        [HttpPost("warehouses/validate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ValidateWarehousesAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var validationError =
                ValidateExcelFile(file);

            if (validationError is not null)
                return validationError;

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            await using var stream =
                file.OpenReadStream();

            var result =
                await _warehouseImportService.ValidateAsync(
                    stream,
                    file.FileName,
                    userId,
                    cancellationToken);

            return Ok(
                ApiResponse<ImportPreviewDto>
                    .SuccessResponse(
                        result,
                        result.CanImport
                            ? "File validated successfully."
                            : "File contains validation errors."));
        }

        [HttpPost("warehouses/{batchId:int}/import")]
        public async Task<IActionResult> ImportWarehousesAsync(
            int batchId,
            CancellationToken cancellationToken)
        {
            var result =
                await _warehouseImportService.ImportAsync(
                    batchId,
                    cancellationToken);

            return Ok(
                ApiResponse<ImportResultDto>
                    .SuccessResponse(
                        result,
                        result.Message));
        }

        [HttpGet("warehouses/{batchId:int}/errors")]
        public async Task<IActionResult> DownloadWarehouseErrorsAsync(
            int batchId,
            CancellationToken cancellationToken)
        {
            var file =
                await _warehouseImportService
                    .GenerateErrorReportAsync(
                        batchId,
                        cancellationToken);

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Salesync-Warehouse-Import-Errors-{batchId}.xlsx");
        }

        [HttpGet("warehouses/history")]
        public async Task<IActionResult> GetWarehouseImportHistoryAsync(
            CancellationToken cancellationToken)
        {
            var result =
                await _warehouseImportService.GetHistoryAsync(
                    cancellationToken);

            return Ok(
                ApiResponse<IEnumerable<ImportBatchHistoryDto>>
                    .SuccessResponse(result));
        }

        // =====================================================
        // PRODUCTS
        // =====================================================

        [HttpGet("products/template")]
        public IActionResult DownloadProductTemplate()
        {
            var file =
                _productImportService.GenerateTemplate();

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Salesync-Products-Template.xlsx");
        }

        [HttpPost("products/validate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ValidateProductsAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var validationError =
                ValidateExcelFile(file);

            if (validationError is not null)
                return validationError;

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            await using var stream =
                file.OpenReadStream();

            var result =
                await _productImportService.ValidateAsync(
                    stream,
                    file.FileName,
                    userId,
                    cancellationToken);

            return Ok(
                ApiResponse<ImportPreviewDto>
                    .SuccessResponse(
                        result,
                        result.CanImport
                            ? "File validated successfully."
                            : "File contains validation errors."));
        }

        [HttpPost("products/{batchId:int}/import")]
        public async Task<IActionResult> ImportProductsAsync(
            int batchId,
            CancellationToken cancellationToken)
        {
            var result =
                await _productImportService.ImportAsync(
                    batchId,
                    cancellationToken);

            return Ok(
                ApiResponse<ImportResultDto>
                    .SuccessResponse(
                        result,
                        result.Message));
        }

        [HttpGet("products/{batchId:int}/errors")]
        public async Task<IActionResult> DownloadProductErrorsAsync(
            int batchId,
            CancellationToken cancellationToken)
        {
            var file =
                await _productImportService
                    .GenerateErrorReportAsync(
                        batchId,
                        cancellationToken);

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Salesync-Product-Import-Errors-{batchId}.xlsx");
        }

        [HttpGet("products/history")]
        public async Task<IActionResult> GetProductImportHistoryAsync(
            CancellationToken cancellationToken)
        {
            var result =
                await _productImportService.GetHistoryAsync(
                    cancellationToken);

            return Ok(
                ApiResponse<IEnumerable<ImportBatchHistoryDto>>
                    .SuccessResponse(result));
        }

        // =====================================================
        // CUSTOMERS
        // =====================================================

        [HttpGet("customers/template")]
        public IActionResult DownloadCustomerTemplate()
        {
            var file =
                _customerImportService.GenerateTemplate();

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Salesync-Customers-Template.xlsx");
        }

        [HttpPost("customers/validate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ValidateCustomersAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var validationError =
                ValidateExcelFile(file);

            if (validationError is not null)
                return validationError;

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            await using var stream =
                file.OpenReadStream();

            var result =
                await _customerImportService.ValidateAsync(
                    stream,
                    file.FileName,
                    userId,
                    cancellationToken);

            return Ok(
                ApiResponse<ImportPreviewDto>
                    .SuccessResponse(
                        result,
                        result.CanImport
                            ? "File validated successfully."
                            : "File contains validation errors."));
        }

        [HttpPost("customers/{batchId:int}/import")]
        public async Task<IActionResult> ImportCustomersAsync(
            int batchId,
            CancellationToken cancellationToken)
        {
            var result =
                await _customerImportService.ImportAsync(
                    batchId,
                    cancellationToken);

            return Ok(
                ApiResponse<ImportResultDto>
                    .SuccessResponse(
                        result,
                        result.Message));
        }

        [HttpGet("customers/{batchId:int}/errors")]
        public async Task<IActionResult> DownloadCustomerErrorsAsync(
            int batchId,
            CancellationToken cancellationToken)
        {
            var file =
                await _customerImportService
                    .GenerateErrorReportAsync(
                        batchId,
                        cancellationToken);

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Salesync-Customer-Import-Errors-{batchId}.xlsx");
        }

        [HttpGet("customers/history")]
        public async Task<IActionResult> GetCustomerImportHistoryAsync(
            CancellationToken cancellationToken)
        {
            var result =
                await _customerImportService.GetHistoryAsync(
                    cancellationToken);

            return Ok(
                ApiResponse<IEnumerable<ImportBatchHistoryDto>>
                    .SuccessResponse(result));
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private IActionResult? ValidateExcelFile(
            IFormFile file)
        {
            if (file is null ||
                file.Length <= 0)
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "Excel file is required."
                    });
            }

            var extension =
                Path.GetExtension(
                    file.FileName);

            if (!extension.Equals(
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "Only .xlsx files are supported."
                    });
            }

            return null;
        }
    }
}