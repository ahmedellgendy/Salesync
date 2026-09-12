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
        private readonly IBranchImportService
            _branchImportService;


        public DataImportController(
            IBranchImportService branchImportService)
        {
            _branchImportService =
                branchImportService;
        }


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
        public async Task<IActionResult>
            ValidateBranchesAsync(
                IFormFile file,
                CancellationToken cancellationToken)
        {
            if (file is null ||
                file.Length <= 0)
            {
                return BadRequest(
                  new
                  {
                      success = false,
                      message = "Excel file is required."
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
                      message = "Only .xlsx files are supported."
                  });
            }


            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            await using var stream =
                file.OpenReadStream();


            var result =
                await _branchImportService
                    .ValidateAsync(
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
        public async Task<IActionResult>
            ImportBranchesAsync(
                int batchId,
                CancellationToken cancellationToken)
        {
            var result =
                await _branchImportService
                    .ImportAsync(
                        batchId,
                        cancellationToken);


            return Ok(
                ApiResponse<ImportResultDto>
                    .SuccessResponse(
                        result,
                        result.Message));
        }


        [HttpGet("branches/{batchId:int}/errors")]
        public async Task<IActionResult>
            DownloadBranchErrorsAsync(
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
        public async Task<IActionResult>
            GetBranchImportHistoryAsync(
                CancellationToken cancellationToken)
        {
            var result =
                await _branchImportService
                    .GetHistoryAsync(
                        cancellationToken);


            return Ok(
                ApiResponse<IEnumerable<ImportBatchHistoryDto>>
                    .SuccessResponse(
                        result));
        }
    }
}