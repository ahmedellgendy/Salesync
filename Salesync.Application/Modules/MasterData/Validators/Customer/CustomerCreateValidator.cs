using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;
using Salesync.Domain.Common.Enums;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                .Matches(@"^(010|011|012|015)[0-9]{8}$")
                .WithMessage("Invalid phone number format.Must be Egyptian number like 01062972156");
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.");
            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Address is required.")
                .MaximumLength(200)
                .WithMessage("Address cannot exceed 200 characters.");
            RuleFor(x => x.CompanyName)
                .MaximumLength(100)
                .WithMessage("Company name cannot exceed 100 characters.")
                .When(x => x.Type == CustomerType.Corporate);
        }
    }
}
