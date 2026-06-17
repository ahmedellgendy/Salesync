using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto;

namespace Salesync.Application.Modules.SalesRep.Validators.SalesRep
{
    public class CreateSalesRepValidator : AbstractValidator<CreateSalesRepDto>
    {
        public CreateSalesRepValidator()
        {
            RuleFor(x => x.SalesRepCode)
                .NotEmpty().WithMessage("SalesRep Code is required.")
                .MaximumLength(15).WithMessage("Phone must not exceed 15 characters.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone is required.")
                .MaximumLength(15).WithMessage("Phone must not exceed 15 characters.")
                .Matches(@"^01[0-9]{9}$").WithMessage("Phone must be a valid Egyptian number.");

            RuleFor(x => x.BranchId)
                .GreaterThan(0);

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.CreditLimit)
                .GreaterThanOrEqualTo(0)
                .When(x => x.CreditLimit.HasValue);
        }

    }
}
