using FluentValidation;
using Salesync.Application.Modules.MasterData.PriceLists.Dtos;

namespace Salesync.Application.Modules.MasterData.PriceLists.Validators;

public class CreatePriceListValidator
    : AbstractValidator<CreatePriceListDto>
{
    public CreatePriceListValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x)
            .Must(x =>
                !x.ValidFrom.HasValue ||
                !x.ValidTo.HasValue ||
                x.ValidTo.Value >= x.ValidFrom.Value)
            .WithMessage(
                "ValidTo must be greater than or equal to ValidFrom.");
    }
}