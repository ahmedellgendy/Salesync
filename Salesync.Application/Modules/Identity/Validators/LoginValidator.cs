using FluentValidation;
using Salesync.Application.Modules.Identity.Dtos.Auth;

namespace Salesync.Application.Modules.Identity.Validators
{
    public class LoginValidator : AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(x=>x.UserName)
                .NotEmpty()
                .WithMessage("Username is required.");

            RuleFor(x=>x.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters.");
        }
    }
}
