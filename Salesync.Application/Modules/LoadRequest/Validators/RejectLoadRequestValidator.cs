using FluentValidation;
using Salesync.Application.Modules.LoadRequest.Dtos;

namespace Salesync.Application.Modules.LoadRequest.Validators
{
    public class RejectLoadRequestValidator : AbstractValidator<RejectLoadRequestDto>
    {
        public RejectLoadRequestValidator()
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .WithMessage("RejectionReason is required.")
                .MaximumLength(500)
                .WithMessage("RejectionReason cannot exceed 500 characters.");
        }
    }
}
