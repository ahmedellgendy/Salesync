using FluentValidation;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;

namespace Salesync.Application.Modules.Sales.Validators.SalesRepDayClosing
{
    public class RejectSalesRepDayClosingValidator : AbstractValidator<RejectSalesRepDayClosingDto>
    {
        public RejectSalesRepDayClosingValidator()
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .WithMessage("Rejection reason is required.")
                .MaximumLength(500)
                .WithMessage("Rejection reason cannot exceed 500 characters.");
        }
    }
}