using FluentValidation;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto;

namespace Salesync.Application.Modules.SalesRep.Validators.SalesRep
{
    public class UpdateSalesRepValidator : AbstractValidator<UpdateSalesRepDto>
    {
        public UpdateSalesRepValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name cannot be empty.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
                .When(x => x.Name != null);

            RuleFor(x => x.Phone)
                .NotEmpty()
                .MaximumLength(15).WithMessage("Phone must not exceed 15 characters.")
                .Matches(@"^01[0-9]{9}$").WithMessage("Phone must be a valid Egyptian number.")
                .When(x => x.Phone != null);

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
