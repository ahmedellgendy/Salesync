using FluentValidation;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;

namespace Salesync.Application.Modules.Sales.Validators.SalesRepDayClosing
{
    public class CreateSalesRepDayClosingValidator : AbstractValidator<CreateSalesRepDayClosingDto>
    {
        public CreateSalesRepDayClosingValidator()
        {
            RuleFor(x => x.SalesRepSessionId)
                .GreaterThan(0)
                .WithMessage("SalesRepSessionId is required.");

            RuleFor(x => x.WarehouseId)
                .GreaterThan(0)
                .WithMessage("WarehouseId is required.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Closing items are required.");

            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
                .WithMessage("Duplicate products are not allowed.")
                .When(x => x.Items != null && x.Items.Any());

            RuleForEach(x => x.Items)
                .SetValidator(new CreateSalesRepDayClosingItemValidator());
        }
    }
}