using FluentValidation;
using Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto;

namespace Salesync.Application.Modules.SalesRep.Validators.RouteCustomer
{
    public class CreateRouteCustomerValidator : AbstractValidator<CreateRouteCustomerDto>
    {
        public CreateRouteCustomerValidator()
        {
            RuleFor(x => x.RouteId)
                .GreaterThan(0);
            RuleFor(x => x.CustomerId)
                .GreaterThan(0);
        }
    }
}