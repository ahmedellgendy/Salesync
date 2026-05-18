namespace Salesync.Application.Modules.MasterData.Dtos.BranchDto
{
    public class BranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
