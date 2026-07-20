using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;

namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class SalesRepMobileTodayDto
    {
        public bool HasOpenSession { get; set; }
        public SalesRepSessionDto? Session { get; set; }
    }
}