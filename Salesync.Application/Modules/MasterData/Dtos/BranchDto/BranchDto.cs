namespace Salesync.Application.Modules.MasterData.Dtos.BranchDto
{
    public class BranchDto
    {
        public int Id { get; set; }
        public required string BranchCode { get; set; } 
        public required string Name { get; set; } 
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
