namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class SalesRepMobileProfileDto
    {
        public int SalesRepId { get; set; }
        public string SalesRepCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public int? BusinessUnitId { get; set; }
    }
}