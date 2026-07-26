using FluentValidation;
using Salesync.Application.Modules.Sales.Dtos.Invoice;

namespace Salesync.Application.Modules.Sales.Validators
{
    public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDto>
    {
        public CreateInvoiceValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("CustomerId is required.");

            RuleFor(x => x.WarehouseId)
                .GreaterThan(0)
                .WithMessage("WarehouseId is required.");

            RuleFor(x => x.SalesRepId)
                .GreaterThan(0)
                .When(x => x.SalesRepId.HasValue)
                .WithMessage("SalesRepId must be greater than zero.");

            RuleFor(x => x.SalesRepSessionId)
                .GreaterThan(0)
                .When(x => x.SalesRepSessionId.HasValue)
                .WithMessage("SalesRepSessionId must be greater than zero.");

            RuleFor(x => x.DiscountAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Invoice discount cannot be negative.");

            RuleFor(x => x.Items)
                .NotNull()
                .WithMessage("Invoice items are required.")
                .NotEmpty()
                .WithMessage("Invoice must contain at least one item.");

            RuleForEach(x => x.Items)
                 .ChildRules(item =>
                 {
                     item.RuleFor(x => x.ProductId)
                         .GreaterThan(0)
                         .WithMessage("ProductId is required.");

                     item.RuleFor(x => x.SaleLargeQuantity)
                         .GreaterThan(0)
                         .WithMessage("Sale large quantity must be greater than zero.");

                     item.RuleFor(x => x.BonusLargeQuantity)
                         .GreaterThanOrEqualTo(0)
                         .WithMessage("Bonus large quantity cannot be negative.");

                     item.RuleFor(x => x.DiscountAmount)
                         .GreaterThanOrEqualTo(0)
                         .WithMessage("Discount amount cannot be negative.");

                     item.RuleFor(x => x.DiscountPercentage)
                         .InclusiveBetween(0, 100)
                         .WithMessage("Discount percentage must be between 0 and 100.");
                 });
        }
    }
}
