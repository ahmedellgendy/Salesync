using SalesRepResponseDto = Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto.SalesRepDto;

namespace Salesync.Application.Modules.SalesRep.Dtos.SalesRepAccount
{
    public sealed class CreatedSalesRepWithAccountDto
    {
        public required SalesRepResponseDto SalesRep { get; set; }

        public required string UserId { get; set; }

        public required string UserName { get; set; }

        public string Role { get; set; } = "SalesRep";
    }
}
