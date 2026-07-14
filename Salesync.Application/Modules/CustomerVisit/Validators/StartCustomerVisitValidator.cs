using FluentValidation;
using Salesync.Application.Modules.CustomerVisit.Dtos;

namespace Salesync.Application.Modules.CustomerVisit.Validators
{
    public class StartCustomerVisitValidator : AbstractValidator<StartCustomerVisitDto>
    {
        public StartCustomerVisitValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("CustomerId is required.");

            RuleFor(x => x.SalesRepId)
                .GreaterThan(0)
                .When(x => x.SalesRepId.HasValue)
                .WithMessage("SalesRepId must be greater than zero.");

            RuleFor(x => x.RouteId)
                .NotNull()
                .WithMessage("Route id is required for customer visit.")
                .GreaterThan(0)
                .WithMessage("Route id must be greater than zero.");

            RuleFor(x => x.SalesRepSessionId)
                .GreaterThan(0)
                .WithMessage("SalesRepSessionId is required.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.Latitude.HasValue)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude.HasValue)
                .WithMessage("Longitude must be between -180 and 180.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}