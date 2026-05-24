using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.ProductDto;

namespace Salesync.Application.Modules.MasterData.Validators.Product
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.ItemCode)
            .MaximumLength(50)
            .When(x => x.ItemCode != null);

            RuleFor(x => x.Name)
                .MaximumLength(150)
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
               .GreaterThanOrEqualTo(0)
               .WithMessage("Unit price must be 0 or more.");

            RuleFor(x => x.CostPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Cost price must be 0 or more.");

            RuleFor(x => x.DiscountPercentage)
                .InclusiveBetween(0, 100)
                .When(x => x.DiscountPercentage.HasValue);

            RuleFor(x => x.Unit)
                .MaximumLength(20)
                .When(x => x.Unit != null);

            RuleFor(x => x.WarehouseId)
                .GreaterThan(0)
                .When(x => x.WarehouseId.HasValue);
            ;
        }
    }
}
