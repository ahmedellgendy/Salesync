using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;

namespace Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto
{
    public class RouteCustomerDetailsDto
    {
        public int Id { get; set; }

        public int RouteId { get; set; }

        public int CustomerId { get; set; }

        public int VisitSequence { get; set; }

        public string? VisitDays { get; set; }

        public string? Notes { get; set; }

        public CustomerDto Customer { get; set; } = null!;
    }
}