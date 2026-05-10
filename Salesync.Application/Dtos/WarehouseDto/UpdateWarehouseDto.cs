namespace Salesync.Application.Dtos.WarehouseDto
{
    public class UpdateWarehouseDto
    {
        public int? BranchId { get; set; }
        public required string Name { get; set; }
        public required string Location { get; set; }
    }
}
