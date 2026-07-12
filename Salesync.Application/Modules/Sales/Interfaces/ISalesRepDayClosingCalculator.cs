using Salesync.Application.Modules.Sales.Models;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public interface ISalesRepDayClosingCalculator
    {
        Task<SalesRepDayClosingCalculationResult> CalculateAsync(CreateSalesRepDayClosingDto dto, SalesRepSession session);

    }
}
