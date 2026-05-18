using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.WarehouseDto;

namespace Salesync.Application.Modules.MasterData.Validators.Warehouse
{
    public class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseDto>
    {
        public UpdateWarehouseValidator()
        {
            RuleFor(x => x.Name)
                 .MaximumLength(100)
                 .WithMessage("Warehouse name cannot exceed 100 characters.")
                 .When(x => x.Name != null);

            RuleFor(x => x.Location)
                .MaximumLength(200)
                .WithMessage("Warehouse location cannot exceed 200 characters.")
                .When(x => x.Location != null);

            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("BranchId must be greater than 0.");
        }
    }
}
