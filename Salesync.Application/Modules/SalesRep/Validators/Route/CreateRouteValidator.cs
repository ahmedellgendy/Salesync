using FluentValidation;
using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;

namespace Salesync.Application.Modules.SalesRep.Validators.Route
{
    public class CreateRouteValidator : AbstractValidator<CreateRouteDto>
    {
        public CreateRouteValidator()
        {
            RuleFor(x => x.RouteCode)
                .NotEmpty().WithMessage("Route Code is required.")
                .MaximumLength(15).WithMessage("Route Code must not exceed 15 characters.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
            RuleFor(x => x.BranchId)
                .GreaterThan(0);
        }
    }
}
