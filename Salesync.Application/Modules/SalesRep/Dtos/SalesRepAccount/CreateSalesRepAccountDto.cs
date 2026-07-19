namespace Salesync.Application.Modules.SalesRep.Dtos.SalesRepAccount
{
    public sealed class CreateSalesRepAccountDto
    {
        public required string UserName { get; set; }

        public required string TemporaryPassword { get; set; }
    }
}
