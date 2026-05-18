using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;

namespace Salesync.Application.Modules.MasterData.Validators.Customer
{
    public class CustomerUpdateValidator : AbstractValidator<UpdateCustomerDto>
    {
        public CustomerUpdateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Invalid email format.");

            RuleFor(x => x.Phone)
                .Matches(@"^(010|011|012|015)[0-9]{8}$")
                .WithMessage("Invalid phone number format.Must be Egyptian number like 01062972156");
        }
    }
}