using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;

namespace Salesync.Application.Modules.MasterData.Validators.Customer
{
    public class CustomerUpdateValidator :
        AbstractValidator<UpdateCustomerDto>
    {
        public CustomerUpdateValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Invalid customer id.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100)
                .When(x => x.Name != null);

            RuleFor(x => x.Phone)
                .Matches(@"^01[0-9]{9}$")
                .When(x =>
                    !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress()
                .MaximumLength(100)
                .When(x =>
                    !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.CreditLimit)
                .GreaterThanOrEqualTo(0)
                .When(x => x.CreditLimit.HasValue);

            RuleFor(x => x.OrderCeiling)
                .GreaterThanOrEqualTo(0)
                .When(x => x.OrderCeiling.HasValue);

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude.HasValue);

            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .When(x => x.BranchId.HasValue);

            RuleFor(x => x.HeadOfficeId)
                .GreaterThan(0)
                .When(x => x.HeadOfficeId.HasValue);

            RuleFor(x => x.Type)
                 .IsInEnum()
                 .When(x => x.Type.HasValue)
                 .WithMessage("Invalid customer type.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage("Invalid customer status.");
        }
    }
}