using Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto;

namespace Salesync.Application.Modules.SalesRep.Dtos.SalesRepAccount
{
    public sealed class CreateSalesRepWithAccountDto
    {
        public required CreateSalesRepDto SalesRep { get; set; }

        public required CreateSalesRepAccountDto Account { get; set; }
    }
}
