using FluentValidation;
using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;

namespace Salesync.Application.Modules.SalesRep.Validators.Route
{
    public class UpdateRouteValidator : AbstractValidator<UpdateRouteDto>
    {
        public UpdateRouteValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name cannot be empty.")
                .MaximumLength(100)
                .When(x => x.Name != null);
        }
    }
}
