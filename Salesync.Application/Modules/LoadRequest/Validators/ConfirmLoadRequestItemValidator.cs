using FluentValidation;
using Salesync.Application.Modules.LoadRequest.Dtos;

namespace Salesync.Application.Modules.LoadRequest.Validators
{
    public class ConfirmLoadRequestItemValidator : AbstractValidator<ConfirmLoadRequestItemDto>
    {
        public ConfirmLoadRequestItemValidator()
        {
            RuleFor(x => x.LoadRequestItemId)
                .GreaterThan(0)
                .WithMessage("LoadRequestItemId is required.");

            RuleFor(x => x.ConfirmedQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("ConfirmedQuantity cannot be negative.");
        }
    }
}