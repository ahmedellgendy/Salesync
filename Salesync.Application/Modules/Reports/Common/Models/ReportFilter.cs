namespace Salesync.Application.Modules.Reports.Common.Models
{
    public class ReportFilter
    {
        public DateOnly FromDate { get; set; }

        public DateOnly ToDate { get; set; }

        public int? SalesRepId { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}