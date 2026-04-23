namespace Salesync.Domain.Entities
{
    public class Customer : BaseEntity
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required string Phone { get; set; }
        public required string Address { get; set; }
        public int RouteId { get; set; }

        public decimal CreditBalance { get; set; }
        public bool HasDebt { get; set; }
    }
}
