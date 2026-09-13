namespace Salesync.Application.Modules.PriceLists.Interfaces
{
    public interface IPricingService
    {
        Task<decimal> GetUnitPriceAsync(
            int customerId,
            int productId);
    }
}