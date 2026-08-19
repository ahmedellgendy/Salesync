using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Modules.Reports.Common.Models;
using Salesync.Application.Modules.Reports.Supervisor.Interfaces;

namespace Salesync.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ISupervisorReportService _supervisorReportService;

        public ReportsController(ISupervisorReportService supervisorReportService)
        {
            _supervisorReportService = supervisorReportService;
        }

        [HttpGet("supervisor/summary")] // GET: api/reports/supervisor/summary
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorSummary([FromQuery] ReportFilter filter,CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetSummaryAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/sales")] // GET: api/reports/supervisor/sales
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorSales([FromQuery] ReportFilter filter,CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetSalesAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/invoices")] // GET: api/reports/supervisor/invoices
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorInvoices([FromQuery] ReportFilter filter,CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetInvoicesAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/payments")] // GET: api/reports/supervisor/payments
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorPayments([FromQuery] ReportFilter filter,CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetPaymentsAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/returns")]  // GET: api/reports/supervisor/returns
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorReturns([FromQuery] ReportFilter filter,CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetReturnsAsync(filter, cancellationToken);

            return Ok(result);
        }

        [HttpGet("supervisor/visits")] // GET: api/reports/supervisor/visits
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorVisits([FromQuery] ReportFilter filter,CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetVisitsAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/salesrep-performance")] // GET: api/reports/supervisor/salesrep-performance
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSalesRepPerformance([FromQuery] ReportFilter filter,CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetSalesRepPerformanceAsync(filter,cancellationToken);

            return Ok(result);
        }

    }
}