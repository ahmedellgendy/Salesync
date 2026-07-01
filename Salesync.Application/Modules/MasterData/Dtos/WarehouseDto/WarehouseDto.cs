using Salesync.Domain.Common.Enums.MasterData;

namespace Salesync.Application.Modules.MasterData.Dtos.WarehouseDto
{
    public class WarehouseDto
    {
        public int Id { get; set; }
        public required string WarehouseCode { get; set; }
        public required string Name { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Location { get; set; }
        public WarehouseType Type { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
