using FluentValidation;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;

namespace Salesync.Application.Modules.Sales.Validators.SalesRepDayClosing
{
    public class CreateSalesRepDayClosingItemValidator : AbstractValidator<CreateSalesRepDayClosingItemDto>
    {
        public CreateSalesRepDayClosingItemValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .WithMessage("ProductId is required.");

            RuleFor(x => x.ActualReturnedQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Actual returned quantity cannot be negative.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}
