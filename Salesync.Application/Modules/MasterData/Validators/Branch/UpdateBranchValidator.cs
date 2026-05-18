using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.BranchDto;

namespace Salesync.Application.Modules.MasterData.Validators.Branch
{
    public class UpdateBranchValidator : AbstractValidator<UpdateBranchDto>
    {
        public UpdateBranchValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters")
                .MinimumLength(2)
                .WithMessage("Name must be at least 2 characters")
                .When(x => x.Name != null);

            RuleFor(x => x.City)
                .MaximumLength(50)
                .WithMessage("City cannot exceed 50 characters")
                .When(x => x.City != null);

            RuleFor(x => x.Address)
                .MaximumLength(200)
                .WithMessage("Address cannot exceed 200 characters")
                .When(x => x.Address != null);

            RuleFor(x => x.Phone)
                .Matches(@"^01[0-9]{9}$")
                .WithMessage("Phone must be a valid Egyptian number (e.g., 01234567890)")
                .When(x => x.Phone != null);
        }
    }
}
