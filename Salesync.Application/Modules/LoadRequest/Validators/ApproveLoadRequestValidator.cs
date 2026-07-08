using FluentValidation;
using Salesync.Application.Modules.LoadRequest.Dtos;

namespace Salesync.Application.Modules.LoadRequest.Validators
{
    public class ApproveLoadRequestValidator : AbstractValidator<ApproveLoadRequestDto>
    {
        public ApproveLoadRequestValidator()
        {
            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Approval must contain at least one item.");

            RuleForEach(x => x.Items)
                .SetValidator(new ApproveLoadRequestItemValidator());
        }
    }
}
