using Salesync.Application.Modules.PriceLists.Interfaces;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Application.Interfaces.Repositories;

namespace Salesync.Application.Modules.PriceLists.Services
{
    public class PricingService : IPricingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PricingService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<decimal> GetUnitPriceAsync(
            int customerId,
            int productId)
        {
            var customer =
                await _unitOfWork.Customers
                    .GetByIdAsync(customerId);

            if (customer is null ||
                !customer.IsActive)
            {
                throw new InvalidOperationException(
                    "Customer was not found or is inactive.");
            }


            var product =
                await _unitOfWork.Products
                    .GetByIdAsync(productId);

            if (product is null ||
                !product.IsActive)
            {
                throw new InvalidOperationException(
                    "Product was not found or is inactive.");
            }


            var today =
                DateTime.UtcNow.Date;


            int? priceListId =
                customer.PriceListId;


            if (!priceListId.HasValue)
            {
                var priceLists =
                    await _unitOfWork.PriceLists
                        .GetAllAsync();

                var defaultPriceList =
                    priceLists
                        .Where(x =>
                            x.IsActive &&
                            x.IsDefault &&
                            IsDateValid(
                                x.ValidFrom,
                                x.ValidTo,
                                today))
                        .OrderByDescending(x =>
                            x.Id)
                        .FirstOrDefault();

                priceListId =
                    defaultPriceList?.Id;
            }
            else
            {
                var priceList =
                    await _unitOfWork.PriceLists
                        .GetByIdAsync(
                            priceListId.Value);

                if (priceList is null ||
                    !priceList.IsActive ||
                    !IsDateValid(
                        priceList.ValidFrom,
                        priceList.ValidTo,
                        today))
                {
                    priceListId =
                        null;
                }
            }


            if (priceListId.HasValue)
            {
                var productPrices =
                    await _unitOfWork.ProductPrices
                        .GetAllAsync();

                var productPrice =
                    productPrices
                        .Where(x =>
                            x.PriceListId ==
                                priceListId.Value
                            &&
                            x.ProductId ==
                                productId
                            &&
                            x.IsActive
                            &&
                            IsDateValid(
                                x.ValidFrom,
                                x.ValidTo,
                                today))
                        .OrderByDescending(x =>
                            x.Id)
                        .FirstOrDefault();


                if (productPrice is not null)
                {
                    return CalculateEffectivePrice(
                        productPrice.UnitPrice,
                        productPrice.DiscountPercentage);
                }
            }


            return product.UnitPrice;
        }


        private static decimal CalculateEffectivePrice(
            decimal unitPrice,
            decimal discountPercentage)
        {
            if (unitPrice <= 0)
            {
                return unitPrice;
            }


            if (discountPercentage <= 0)
            {
                return decimal.Round(
                    unitPrice,
                    2,
                    MidpointRounding.AwayFromZero);
            }


            if (discountPercentage > 100)
            {
                throw new InvalidOperationException(
                    "Product price discount percentage cannot exceed 100%.");
            }


            var discountAmount =
                unitPrice *
                discountPercentage /
                100m;


            var finalPrice =
                unitPrice -
                discountAmount;


            return decimal.Round(
                finalPrice,
                2,
                MidpointRounding.AwayFromZero);
        }


        private static bool IsDateValid(
            DateTime? validFrom,
            DateTime? validTo,
            DateTime date)
        {
            if (validFrom.HasValue &&
                validFrom.Value.Date > date)
            {
                return false;
            }


            if (validTo.HasValue &&
                validTo.Value.Date < date)
            {
                return false;
            }


            return true;
        }
    }
}