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

            When(
                x => x.Source == TreasuryTransactionSource.Expense,
                () =>
                {
                    RuleFor(x => x.ExpenseCategoryId)
                        .NotNull()
                        .WithMessage(
                            "Expense category is required for expense transactions.");

                    RuleFor(x => x.ExpenseCategoryId)
                        .GreaterThan(0)
                        .When(x => x.ExpenseCategoryId.HasValue)
                        .WithMessage(
                            "Expense category id must be greater than zero.");
                });

            When(
                x => x.Source != TreasuryTransactionSource.Expense,
                () =>
                {
                    RuleFor(x => x.ExpenseCategoryId)
                        .Null()
                        .WithMessage(
                            "Expense category can only be used for expense transactions.");
                });

            RuleFor(x => x.ReferenceNumber)
                .MaximumLength(100);

            RuleFor(x => x.Notes)
                .MaximumLength(500);
        }
    }
}