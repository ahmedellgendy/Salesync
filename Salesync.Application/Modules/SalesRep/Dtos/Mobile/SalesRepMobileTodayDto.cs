using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;

namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class SalesRepMobileTodayDto
    {
        public bool HasTodaySession { get; set; }
        public bool HasOpenSession { get; set; }
        public bool IsDayClosed { get; set; }

        public SalesRepSessionDto? Session { get; set; }

        public int TotalCustomers { get; set; }
        public int VisitedCustomers { get; set; }
        public int RemainingCustomers { get; set; }

        public int InvoicesCount { get; set; }
        public decimal SalesTotal { get; set; }

        public decimal PaidTotal { get; set; }
        public decimal RemainingTotal { get; set; }

        public int PaymentsCount { get; set; }
    }
}