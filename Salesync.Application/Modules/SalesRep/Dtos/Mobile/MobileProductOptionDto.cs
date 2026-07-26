namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class MobileProductOptionDto
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
    }
}