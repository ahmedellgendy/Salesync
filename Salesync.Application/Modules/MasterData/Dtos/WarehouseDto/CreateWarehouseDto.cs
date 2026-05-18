namespace Salesync.Application.Modules.MasterData.Dtos.WarehouseDto
{
    public class CreateWarehouseDto
    {
        public int BranchId { get; set; }
        public required string Name { get; set; }
        public required string Location { get; set; }
    }
}
