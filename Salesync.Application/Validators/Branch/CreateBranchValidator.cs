using FluentValidation;
using Salesync.Application.Dtos.BranchDto;

namespace Salesync.Application.Validators.Branch
{
    public class CreateBranchValidator : AbstractValidator<CreateBranchDto>
    {
        public CreateBranchValidator()
        {

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Branch name is required.")
                .MaximumLength(100)
                .WithMessage("Branch name cannot exceed 100 characters.");
            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Branch location is required.")
                .MaximumLength(200)
                .WithMessage("Branch location cannot exceed 200 characters.");
            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithMessage("Branch phone number is required.")
                .Matches(@"^(010|011|012|015)[0-9]{8}$")
                .WithMessage("Invalid phone number format.Must be Egyptian number like 01062972156");
            RuleFor(x => x.City)
                 .NotEmpty()
                 .WithMessage("Branch city is required.")
                 .MaximumLength(100)
                 .WithMessage("Branch city cannot exceed 100 characters.");
        }
    }
}
