using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;

namespace Salesync.Application.Modules.MasterData.Validators.Customer
{
    public class CustomerCreateValidator :
        AbstractValidator<CreateCustomerDto>
    {
        public CustomerCreateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Customer name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Phone)
                .Matches(@"^01[0-9]{9}$")
                .WithMessage(
                    "Invalid Egyptian phone number format.")
                .When(x =>
                    !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .MaximumLength(100)
                .When(x =>
                    !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Address)
                .MaximumLength(200)
                .When(x => x.Address != null);

            RuleFor(x => x.CreditLimit)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.OrderCeiling)
                .GreaterThanOrEqualTo(0);

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

            RuleFor(x => x)
                .Must(x =>
                    !x.IsHeadOffice ||
                    !x.HeadOfficeId.HasValue)
                .WithMessage(
                    "A head office customer cannot belong to another head office.");

            RuleFor(x => x.Type)
              .IsInEnum()
              .WithMessage("Invalid customer type.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid customer status.");
        }
    }
}