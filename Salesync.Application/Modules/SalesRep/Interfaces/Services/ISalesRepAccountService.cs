using Salesync.Application.Modules.SalesRep.Dtos.SalesRepAccount;

namespace Salesync.Application.Modules.SalesRep.Interfaces.Services
{
    public interface ISalesRepAccountService
    {
        Task<CreatedSalesRepWithAccountDto> CreateAsync(CreateSalesRepWithAccountDto dto);
    }
}