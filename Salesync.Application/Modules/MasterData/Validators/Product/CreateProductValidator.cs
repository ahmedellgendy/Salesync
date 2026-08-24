using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.ProductDto;

namespace Salesync.Application.Modules.MasterData.Validators.Product
{
    public class CreateProductValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.ItemCode)
                .NotEmpty()
                .WithMessage("Item code is required.")
                .MaximumLength(50)
                .WithMessage("Item code cannot exceed 50 characters.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Product name is required.")
                .MaximumLength(100)
                .WithMessage("Product name cannot exceed 100 characters.");

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
                .WithMessage("Unit price must be greater than 0.");

            RuleFor(x => x.CostPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Cost price must be 0 or more.");

            RuleFor(x => x)
                .Must(x => x.CostPrice <= x.UnitPrice)
                .WithMessage("Cost price cannot be greater than unit price.");

            RuleFor(x => x.DiscountPercentage)
                .InclusiveBetween(0, 100)
                .When(x => x.DiscountPercentage.HasValue);

            RuleFor(x => x.Unit)
                .MaximumLength(20)
                .When(x => x.Unit != null);

            RuleFor(x => x.SmallUnit)
                .NotEmpty()
                .WithMessage("Small unit is required.")
                .MaximumLength(50);

            RuleFor(x => x.LargeUnit)
                .NotEmpty()
                .WithMessage("Large unit is required.")
                .MaximumLength(50);

            RuleFor(x => x.UnitsPerLargeUnit)
                .GreaterThan(0)
                .WithMessage("Units per large unit must be greater than 0.");

            RuleFor(x => x.MinStockLevel)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.MaxStockLevel)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x)
                .Must(x =>
                    x.MaxStockLevel == 0 ||
                    x.MaxStockLevel >= x.MinStockLevel)
                .WithMessage(
                    "Max stock must be greater than or equal to min stock.");

            RuleFor(x => x.ReturnPeriod)
                .GreaterThan(0)
                .When(x =>
                    x.EnableReturn &&
                    x.ReturnPeriod.HasValue);

            RuleFor(x => x.WarehouseId)
                .GreaterThan(0)
                .When(x => x.WarehouseId.HasValue);
        }
    }
}