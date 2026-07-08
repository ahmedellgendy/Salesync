using FluentValidation;
using Salesync.Application.Modules.LoadRequest.Dtos;

namespace Salesync.Application.Modules.LoadRequest.Validators
{
    public class CreateLoadRequestValidator : AbstractValidator<CreateLoadRequestDto>
    {
        public CreateLoadRequestValidator()
        {
            RuleFor(x => x.WarehouseId)
                .GreaterThan(0)
                .WithMessage("WarehouseId is required.");

            RuleFor(x => x.SalesRepId)
                .GreaterThan(0)
                .When(x => x.SalesRepId.HasValue)
                .WithMessage("SalesRepId must be greater than zero.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Load request must contain at least one item.");

            RuleForEach(x => x.Items)
                .SetValidator(new CreateLoadRequestItemValidator());
        }
    }
}