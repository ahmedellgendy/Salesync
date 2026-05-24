using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;

namespace Salesync.Application.Modules.MasterData.Validators.Customer
{
    public class CustomerCreateValidator : AbstractValidator<CreateCustomerDto>
    {
        public CustomerCreateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Customer name is required.")
                .MaximumLength(100)
                .WithMessage("Customer name cannot exceed 100 characters.");

            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithMessage("Phone number is required.")
                .Matches(@"^(01)[0-9]{9}$")
                .WithMessage("Invalid phone number format.Must be Egyptian number like 01062972156");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Address is required.")
                .MaximumLength(200)
                .WithMessage("Address cannot exceed 200 characters.");
           
            RuleFor(x => x.CreditLimit)
                 .GreaterThanOrEqualTo(0)
                 .WithMessage("Credit limit cannot be negative.");
           
            RuleFor(x => x.OrderCeiling)
                .GreaterThan(0)
                .WithMessage("Order ceiling must be greater than 0.")
                .GreaterThanOrEqualTo(x => x.CreditLimit)
                .WithMessage("Order ceiling must be >= credit limit.");
        }
    }
}
