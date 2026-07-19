using FluentValidation;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepAccount;
using CreateSalesRepRequest = Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto.CreateSalesRepDto;

namespace Salesync.Application.Modules.SalesRep.Validators.SalesRepAccount
{
    public sealed class CreateSalesRepWithAccountValidator : AbstractValidator<CreateSalesRepWithAccountDto>
    {
        public CreateSalesRepWithAccountValidator(IValidator<CreateSalesRepRequest> salesRepValidator)
        {
            RuleFor(x => x.SalesRep)
                .NotNull()
                .WithMessage("Sales representative data is required.")
                .SetValidator(salesRepValidator);

            RuleFor(x => x.Account)
                .NotNull()
                .WithMessage("Account data is required.");

            When(x => x.Account is not null, () =>
            {
                RuleFor(x => x.Account.UserName)
                    .NotEmpty()
                    .WithMessage("Username is required.")
                    .MinimumLength(4)
                    .WithMessage("Username must be at least 4 characters.")
                    .MaximumLength(100)
                    .WithMessage("Username cannot exceed 100 characters.")
                    .Matches(@"^[a-zA-Z0-9._-]+$")
                    .WithMessage(
                        "Username can only contain letters, numbers, dots, underscores, and hyphens.");

                RuleFor(x => x.Account.TemporaryPassword)
                    .NotEmpty()
                    .WithMessage("Temporary password is required.")
                    .MinimumLength(8)
                    .WithMessage(
                        "Temporary password must be at least 8 characters.")
                    .MaximumLength(100)
                    .WithMessage(
                        "Temporary password cannot exceed 100 characters.");
            });
        }
    }
}