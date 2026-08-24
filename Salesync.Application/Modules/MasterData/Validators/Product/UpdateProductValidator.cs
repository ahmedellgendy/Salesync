using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.ProductDto;

namespace Salesync.Application.Modules.MasterData.Validators.Product
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Product id must be greater than 0.");

            RuleFor(x => x.ItemCode)
                .NotEmpty()
                .MaximumLength(50)
                .When(x => x.ItemCode != null);

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100)
                .When(x => x.Name != null);

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => x.Description != null);

            RuleFor(x => x.SKU)
                .MaximumLength(50)
                .When(x => x.SKU != null);

            RuleFor(x => x.Barcode)
                .MaximumLength(50)
                .When(x => x.Barcode != null);

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0)
                .When(x => x.UnitPrice.HasValue);

            RuleFor(x => x.CostPrice)
                .GreaterThanOrEqualTo(0)
                .When(x => x.CostPrice.HasValue);

            RuleFor(x => x.DiscountPercentage)
                .InclusiveBetween(0, 100)
                .When(x => x.DiscountPercentage.HasValue);

            RuleFor(x => x.Unit)
                .MaximumLength(20)
                .When(x => x.Unit != null);

            RuleFor(x => x.SmallUnit)
                .NotEmpty()
                .MaximumLength(50)
                .When(x => x.SmallUnit != null);

            RuleFor(x => x.LargeUnit)
                .NotEmpty()
                .MaximumLength(50)
                .When(x => x.LargeUnit != null);

            RuleFor(x => x.UnitsPerLargeUnit)
                .GreaterThan(0)
                .When(x => x.UnitsPerLargeUnit.HasValue);

            RuleFor(x => x.MinStockLevel)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MinStockLevel.HasValue);

            RuleFor(x => x.MaxStockLevel)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MaxStockLevel.HasValue);

            RuleFor(x => x.ReturnPeriod)
                .GreaterThan(0)
                .When(x => x.ReturnPeriod.HasValue);

            RuleFor(x => x.WarehouseId)
                .GreaterThan(0)
                .When(x => x.WarehouseId.HasValue);
        }
    }
}