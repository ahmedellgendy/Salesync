using FluentValidation;
using Salesync.Application.Modules.Treasury.Dtos;

namespace Salesync.Application.Modules.Treasury.Validators
{
    public class ReceiveDayClosingCashValidator : AbstractValidator<ReceiveDayClosingCashDto>
    {
        public ReceiveDayClosingCashValidator()
        {
            RuleFor(x => x.CashBoxId)
                .GreaterThan(0)
                .WithMessage("Cash box id must be greater than zero.");

            RuleFor(x => x.ReceivedAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Received amount cannot be negative.");

            RuleFor(x => x.Notes)
                .MaximumLength(500);
        }
    }
}