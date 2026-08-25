using FluentValidation;
using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;

namespace Salesync.Application.Modules.SalesRep.Validators.Route
{
    public class CreateRouteValidator : AbstractValidator<CreateRouteDto>
    {
        public CreateRouteValidator()
        {
            RuleFor(x => x.RouteCode)
                .NotEmpty()
                .WithMessage("Route Code is required.")
                .MaximumLength(15)
                .WithMessage("Route Code must not exceed 15 characters.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("BranchId must be greater than 0.");

            RuleFor(x => x.AssignedSalesRepId)
                .GreaterThan(0)
                .When(x => x.AssignedSalesRepId.HasValue);

            RuleFor(x => x.Type)
                .MaximumLength(50)
                .When(x => x.Type != null);

            RuleFor(x => x.RegionCode)
                .MaximumLength(50)
                .When(x => x.RegionCode != null);

            RuleFor(x => x.DistrictCode)
                .MaximumLength(50)
                .When(x => x.DistrictCode != null);

            RuleFor(x => x.CityCode)
                .MaximumLength(50)
                .When(x => x.CityCode != null);

            RuleFor(x => x.AreaCode)
                .MaximumLength(50)
                .When(x => x.AreaCode != null);
        }
    }
}