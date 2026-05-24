namespace Salesync.Application.Modules.MasterData.Dtos.BranchDto
{
    public class CreateBranchDto
    {
        public required string BranchCode { get; set; }
        public required string Name { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }
}
