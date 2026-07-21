using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;

namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class SalesRepMobileTodayDto
    {
        public bool HasTodaySession { get; set; }

        public bool HasOpenSession { get; set; }

        public bool IsDayClosed { get; set; }

        public SalesRepSessionDto? Session { get; set; }
    }
}