using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Modules.Reports.Admin.Interfaces;
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
        private readonly IAdminReportService _adminReportService;

        public ReportsController(ISupervisorReportService supervisorReportService, IAdminReportService adminReportService)
        {
            _supervisorReportService = supervisorReportService;
            _adminReportService = adminReportService;
        }

        #region Supervisor Endpoints 

        [HttpGet("supervisor/summary")] // GET: api/reports/supervisor/summary
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorSummary([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetSummaryAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/sales")] // GET: api/reports/supervisor/sales
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorSales([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetSalesAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/invoices")] // GET: api/reports/supervisor/invoices
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorInvoices([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetInvoicesAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/payments")] // GET: api/reports/supervisor/payments
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorPayments([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetPaymentsAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/returns")]  // GET: api/reports/supervisor/returns
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorReturns([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetReturnsAsync(filter, cancellationToken);

            return Ok(result);
        }

        [HttpGet("supervisor/visits")] // GET: api/reports/supervisor/visits
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSupervisorVisits([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetVisitsAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("supervisor/salesrep-performance")] // GET: api/reports/supervisor/salesrep-performance
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> GetSalesRepPerformance([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _supervisorReportService.GetSalesRepPerformanceAsync(filter, cancellationToken);

            return Ok(result);
        }

        #endregion

        [HttpGet("admin/summary")] // GET: api/reports/admin/summary
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminSummary([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _adminReportService.GetSummaryAsync(filter, cancellationToken);

            return Ok(result);
        }

        [HttpGet("admin/sales")]  // GET: api/reports/admin/sales
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminSales([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _adminReportService.GetSalesAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("admin/invoices")] // GET: api/reports/admin/invoices
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminInvoices([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _adminReportService.GetInvoicesAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("admin/payments")] // GET: api/reports/admin/payments
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPayments([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _adminReportService.GetPaymentsAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("admin/returns")] // GET: api/reports/admin/returns
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminReturns([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _adminReportService.GetReturnsAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("admin/visits")] // GET: api/reports/admin/visits
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminVisits([FromQuery] ReportFilter filter, CancellationToken cancellationToken)
        {
            var result = await _adminReportService.GetVisitsAsync(filter, cancellationToken);
            return Ok(result);
        }
    }
}