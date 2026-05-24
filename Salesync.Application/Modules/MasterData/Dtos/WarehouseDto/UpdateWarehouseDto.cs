using Salesync.Domain.Common.Enums;

namespace Salesync.Application.Modules.MasterData.Dtos.WarehouseDto
{
    public class UpdateWarehouseDto
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
        public int? BranchId { get; set; }
        public WarehouseType Type { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
       
    }
}
