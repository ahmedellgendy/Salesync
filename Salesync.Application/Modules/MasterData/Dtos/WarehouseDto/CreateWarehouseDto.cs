using Salesync.Domain.Common.Enums;

namespace Salesync.Application.Modules.MasterData.Dtos.WarehouseDto
{
    public class CreateWarehouseDto
    {
        public required string WarehouseCode { get; set; }
        public required string Name { get; set; }
        public int BranchId { get; set; }
        public string? Location { get; set; }
        public WarehouseType Type { get; set; } = WarehouseType.Main;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        
    }
}
