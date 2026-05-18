namespace Salesync.Application.Modules.MasterData.Dtos.WarehouseDto
{
    public class WarehouseDto
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public required string BranchName { get; set; } 
        public required string Name { get; set; } 
        public required string Location { get; set; } 
    }
}
