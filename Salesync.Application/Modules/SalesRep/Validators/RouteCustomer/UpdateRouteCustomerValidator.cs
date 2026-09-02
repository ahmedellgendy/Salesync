using FluentValidation;
using Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto;

namespace Salesync.Application.Modules.SalesRep.Validators.RouteCustomer
{
    public class UpdateRouteCustomerValidator
        : AbstractValidator<UpdateRouteCustomerDto>
    {
        public UpdateRouteCustomerValidator()
        {
            RuleFor(x => x.VisitSequence)
                .GreaterThan(0)
                .When(x => x.VisitSequence.HasValue);

            RuleFor(x => x.VisitDays)
                .MaximumLength(100)
                .When(x => x.VisitDays != null);

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .When(x => x.Notes != null);
        }
    }
}