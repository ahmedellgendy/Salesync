namespace Salesync.Application.Modules.Identity.Dtos.User
{
    public class UpdateUserDto
    {
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public int? BranchId { get; set; }
        public int? BusinessUnitId { get; set; }
        public bool? IsActive { get; set; }
    }
}
