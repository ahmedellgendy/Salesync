using FluentValidation;
using Salesync.Application.Modules.MasterData.PriceLists.Dtos;

namespace Salesync.Application.Modules.MasterData.PriceLists.Validators;

public class UpdateProductPriceValidator
    : AbstractValidator<UpdateProductPriceDto>
{
    public UpdateProductPriceValidator()
    {
        RuleFor(x => x.UnitPrice)
            .GreaterThan(0);

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 100);

        RuleFor(x => x)
            .Must(x =>
                !x.ValidFrom.HasValue ||
                !x.ValidTo.HasValue ||
                x.ValidTo.Value >= x.ValidFrom.Value)
            .WithMessage(
                "ValidTo must be greater than or equal to ValidFrom.");
    }
}