using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;

namespace Salesync.Application.Modules.MasterData.Validators.Customer
{
    public class CustomerUpdateValidator : AbstractValidator<UpdateCustomerDto>
    {
        public CustomerUpdateValidator()
        {
            RuleFor(x => x.Id)
               .GreaterThan(0)
               .WithMessage("Invalid customer Id.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Phone)
                .Matches(@"^(01)[0-9]{9}$")
                .WithMessage("Invalid Egyptian phone number format.")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));
        }
    }
}