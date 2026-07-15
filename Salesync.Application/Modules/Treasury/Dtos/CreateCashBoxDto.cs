namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class CreateCashBoxDto
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        public string Currency { get; set; } = "EGP";

        public string? Notes { get; set; }
    }
}
