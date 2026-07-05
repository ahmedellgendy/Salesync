using FluentValidation;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Validators
{
    public class CreatePaymentValidator : AbstractValidator<CreatePaymentDto>
    {
        public CreatePaymentValidator()
        {
            RuleFor(x => x.InvoiceId)
                .GreaterThan(0)
                .WithMessage("InvoiceId is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Payment amount must be greater than zero.");

            RuleFor(x => x.SalesRepId)
                .GreaterThan(0)
                .When(x => x.SalesRepId.HasValue)
                .WithMessage("SalesRepId must be greater than zero.");

            RuleFor(x => x.SalesRepSessionId)
                .GreaterThan(0)
                .When(x => x.SalesRepSessionId.HasValue)
                .WithMessage("SalesRepSessionId must be greater than zero.");

            RuleFor(x => x.CheckNumber)
                .NotEmpty()
                .When(x => x.PaymentMethod == PaymentMethod.Check)
                .WithMessage("Check number is required for check payments.");

            RuleFor(x => x.CheckDueDate)
                .NotNull()
                .When(x => x.PaymentMethod == PaymentMethod.Check)
                .WithMessage("Check due date is required for check payments.");

            RuleFor(x => x.BankName)
                .NotEmpty()
                .When(x => x.PaymentMethod == PaymentMethod.Check)
                .WithMessage("Bank name is required for check payments.");

            RuleFor(x => x.TransactionReference)
                .NotEmpty()
                .When(x => x.PaymentMethod == PaymentMethod.BankTransfer)
                .WithMessage("Transaction reference is required for bank transfer payments.");

        }
    }
}
