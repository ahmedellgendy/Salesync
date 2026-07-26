namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class MobileWarehouseOptionDto
    {
        public int Id { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string? Location { get; set; }
    }
}