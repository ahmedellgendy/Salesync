using FluentValidation;
using Salesync.Application.Dtos.WarehouseDto;

namespace Salesync.Application.Validators.Warehouse
{
    public class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseDto>
    {
        public UpdateWarehouseValidator() 
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Warehouse name is required.")
                .MaximumLength(100)
                .WithMessage("Warehouse name cannot exceed 100 characters.");
            RuleFor(x => x.Location)
                .NotEmpty()
                .WithMessage("Warehouse location is required.")
                .MaximumLength(200)
                .WithMessage("Warehouse location cannot exceed 200 characters.");
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("BranchId must be greater than 0.");
        }
    }
}
