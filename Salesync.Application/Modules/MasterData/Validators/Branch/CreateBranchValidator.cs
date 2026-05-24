using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.BranchDto;

namespace Salesync.Application.Modules.MasterData.Validators.Branch
{
    public class CreateBranchValidator : AbstractValidator<CreateBranchDto>
    {
        public CreateBranchValidator()
        {
            RuleFor(x => x.BranchCode)
                .NotEmpty()
                .WithMessage("Branch code is required.")
                .MaximumLength(50)
                .WithMessage("Branch code cannot exceed 50 characters.");
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Branch name is required.")
                .MaximumLength(100)
                .WithMessage("Branch name cannot exceed 100 characters.");
            RuleFor(x => x.Phone)
                .Matches(@"^(010|011|012|015)[0-9]{8}$")
                .WithMessage("Invalid phone number format.Must be Egyptian number like 01062972156")
                .When(x =>!string.IsNullOrEmpty(x.Phone));
        }
    }
}
