using FluentValidation;
using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;

namespace Salesync.Application.Modules.SalesRep.Validators.Route
{
    public class UpdateRouteValidator : AbstractValidator<UpdateRouteDto>
    {
        public UpdateRouteValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name cannot be empty.")
                .MaximumLength(100)
                .When(x => x.Name != null);

            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .When(x => x.BranchId.HasValue);

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