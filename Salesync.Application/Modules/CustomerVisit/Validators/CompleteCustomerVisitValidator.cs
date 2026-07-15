using FluentValidation;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Domain.Common.Enums.CustomerVisit;

namespace Salesync.Application.Modules.CustomerVisit.Validators
{
    public class CompleteCustomerVisitValidator : AbstractValidator<CompleteCustomerVisitDto>
    {
        public CompleteCustomerVisitValidator()
        {
            RuleFor(x => x.VisitType)
                .IsInEnum()
                .WithMessage("Invalid visit type.");

            RuleFor(x => x.NegativeReason)
                .NotNull()
                .When(x => x.VisitType == VisitType.Negative)
                .WithMessage("NegativeReason is required for negative visits.");

            RuleFor(x => x.NegativeReason)
                .IsInEnum()
                .When(x => x.NegativeReason.HasValue)
                .WithMessage("Invalid negative reason.");

            RuleFor(x => x.NegativeReason)
                .Null()
                .When(x => x.VisitType == VisitType.Positive)
                .WithMessage("NegativeReason must be empty for positive visits.");

            RuleFor(x => x)
                .Must(x =>
                    x.InvoiceId.HasValue ||
                    x.PaymentId.HasValue ||
                    x.InvoiceReturnId.HasValue)
                .When(x => x.VisitType == VisitType.Positive)
                .WithMessage("Positive visit must be linked to invoice, payment, or return.");

            RuleFor(x => x)
                .Must(x =>
                    !x.InvoiceId.HasValue &&
                    !x.PaymentId.HasValue &&
                    !x.InvoiceReturnId.HasValue)
                .When(x => x.VisitType == VisitType.Negative)
                .WithMessage("Negative visit cannot be linked to invoice, payment, or return.");

            RuleFor(x => x.InvoiceId)
                .GreaterThan(0)
                .When(x => x.InvoiceId.HasValue)
                .WithMessage("InvoiceId must be greater than zero.");

            RuleFor(x => x.PaymentId)
                .GreaterThan(0)
                .When(x => x.PaymentId.HasValue)
                .WithMessage("PaymentId must be greater than zero.");

            RuleFor(x => x.InvoiceReturnId)
                .GreaterThan(0)
                .When(x => x.InvoiceReturnId.HasValue)
                .WithMessage("InvoiceReturnId must be greater than zero.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}
