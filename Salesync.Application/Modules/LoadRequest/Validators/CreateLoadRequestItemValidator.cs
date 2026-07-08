using FluentValidation;
using Salesync.Application.Modules.LoadRequest.Dtos;

namespace Salesync.Application.Modules.LoadRequest.Validators
{
    public class CreateLoadRequestItemValidator : AbstractValidator<CreateLoadRequestItemDto>
    {
        public CreateLoadRequestItemValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .WithMessage("ProductId is required.");

            RuleFor(x => x.RequestedQuantity)
                .GreaterThan(0)
                .WithMessage("RequestedQuantity must be greater than zero.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}
