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
                .WithMessage("Customer name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.SKU)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Barcode)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0);

            RuleFor(x => x.CostPrice)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(x => x.UnitPrice)
                .WithMessage("Cost price cannot be greater than unit price.");

            RuleFor(x => x.DiscountPercentage)
                .InclusiveBetween(0, 100)
                .When(x => x.DiscountPercentage.HasValue);

            RuleFor(x => x.MinStockLevel)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.MaxStockLevel)
                .GreaterThanOrEqualTo(0)
                .GreaterThanOrEqualTo(x => x.MinStockLevel)
                .WithMessage("Max stock must be greater than or equal to min stock.")
                .When(x => x.MaxStockLevel > 0);

            RuleFor(x => x.Unit)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.WarehouseId)
                .GreaterThan(0)
                .When(x => x.WarehouseId.HasValue);

            ;

        }
    }
}
