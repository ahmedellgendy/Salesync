using FluentValidation;
using Salesync.Application.Modules.LoadRequest.Dtos;

namespace Salesync.Application.Modules.LoadRequest.Validators
{
    public class ApproveLoadRequestItemValidator : AbstractValidator<ApproveLoadRequestItemDto>
    {
        public ApproveLoadRequestItemValidator()
        {
            RuleFor(x => x.LoadRequestItemId)
                .GreaterThan(0)
                .WithMessage("LoadRequestItemId is required.");

            RuleFor(x => x.ApprovedQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("ApprovedQuantity cannot be negative.");
        }
    }
}