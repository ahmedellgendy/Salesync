using FluentValidation;
using Salesync.Application.Modules.Inventory.Dtos;

namespace Salesync.Application.Modules.Inventory.Validators
{
    public class CreateOpeningBalanceValidator : AbstractValidator<CreateOpeningBalanceDto>
    {
        public CreateOpeningBalanceValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .WithMessage("ProductId is required.");

            RuleFor(x => x.WarehouseId)
                .GreaterThan(0)
                .WithMessage("WarehouseId is required.");

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Quantity cannot be negative.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}