using FluentValidation;
using Salesync.Application.Modules.MasterData.Dtos.WarehouseDto;

namespace Salesync.Application.Modules.MasterData.Validators.Warehouse
{
    public class CreateWarehouseValidator : AbstractValidator<CreateWarehouseDto>
    {
        public CreateWarehouseValidator()
        {
            RuleFor(x => x.WarehouseCode)
                .NotEmpty()
                .WithMessage("Warehouse code is required.")
                .MaximumLength(50)
                .WithMessage("Warehouse code cannot exceed 50 characters.");
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Warehouse name is required.")
                .MaximumLength(100)
                .WithMessage("Warehouse name cannot exceed 100 characters.");
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("BranchId must be greater than 0.");
        }
    }
}
