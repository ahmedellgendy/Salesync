using FluentValidation;
using Salesync.Application.Modules.Treasury.Dtos;

namespace Salesync.Application.Modules.Treasury.Validators
{
    public class CreateCashBoxValidator : AbstractValidator<CreateCashBoxDto>
    {
        public CreateCashBoxValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Cash box code is required.")
                .MaximumLength(50);

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Cash box name is required.")
                .MaximumLength(150);

            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .When(x => x.BranchId.HasValue)
                .WithMessage("Branch id must be greater than zero.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required.")
                .MaximumLength(10);

            RuleFor(x => x.Notes)
                .MaximumLength(500);
        }
    }
}