using FluentValidation;
using Salesync.Application.Modules.Treasury.Dtos;

namespace Salesync.Application.Modules.Treasury.Validators
{
    public class CreateExpenseCategoryDtoValidator
        : AbstractValidator<CreateExpenseCategoryDto>
    {
        public CreateExpenseCategoryDtoValidator()
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