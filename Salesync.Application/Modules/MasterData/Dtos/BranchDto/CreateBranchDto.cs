namespace Salesync.Application.Modules.MasterData.Dtos.BranchDto
{
    public class CreateBranchDto
    {
        public required string Name { get; set; }
        public required string City { get; set; }
        public required string Address { get; set; }
        public required string Phone { get; set; }
    }
}
