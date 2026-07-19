namespace Salesync.Application.Modules.Identity.Dtos.User
{
    public sealed class CreateSalesRepUserDto
    {
        public required string UserName { get; set; }

        public required string Password { get; set; }

        public required string FullName { get; set; }

        public required string PhoneNumber { get; set; }

        public string? Email { get; set; }

        public int? BranchId { get; set; }

        public int? BusinessUnitId { get; set; }
    }
}