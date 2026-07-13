namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class CashBoxDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        public string Currency { get; set; } = "EGP";

        public decimal CurrentBalance { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; }
    }
}