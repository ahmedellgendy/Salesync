using FluentValidation;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;

namespace Salesync.Application.Modules.Sales.Validators.SalesRepDayClosing
{
    public class CreateSalesRepDayClosingValidator
        : AbstractValidator<CreateSalesRepDayClosingDto>
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
        }
    }
}