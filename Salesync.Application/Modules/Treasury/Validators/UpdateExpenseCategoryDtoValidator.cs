using FluentValidation;
using Salesync.Application.Modules.Treasury.Dtos;

namespace Salesync.Application.Modules.Treasury.Validators
{
    public class UpdateExpenseCategoryDtoValidator
        : AbstractValidator<UpdateExpenseCategoryDto>
    {
        public UpdateExpenseCategoryDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .MaximumLength(500);
        }
    }
}