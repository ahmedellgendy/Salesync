namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class UpdateExpenseCategoryDto
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}