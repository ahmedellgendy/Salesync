using FluentValidation;
using Salesync.Application.Modules.Treasury.Dtos;
using Salesync.Domain.Common.Enums.Treasury;

namespace Salesync.Application.Modules.Treasury.Validators
{
    public class CreateTreasuryCashOutDtoValidator
        : AbstractValidator<CreateTreasuryCashOutDto>
    {
        public CreateTreasuryCashOutDtoValidator()
        {
            RuleFor(x => x.CashBoxId)
                .GreaterThan(0);

            RuleFor(x => x.Amount)
                .GreaterThan(0);

            RuleFor(x => x.Source)
                .Must(source =>
                    source == TreasuryTransactionSource.Expense ||
                    source == TreasuryTransactionSource.BankDeposit ||
                    source == TreasuryTransactionSource.Adjustment ||
                    source == TreasuryTransactionSource.TransferOut)
                .WithMessage(
                    "Invalid source for cash out transaction.");

            RuleFor(x => x.ReferenceNumber)
                .MaximumLength(100);

            RuleFor(x => x.Notes)
                .MaximumLength(500);
        }
    }
}