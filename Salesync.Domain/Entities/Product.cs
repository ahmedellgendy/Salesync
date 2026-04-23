namespace Salesync.Domain.Entities
{
    public class Product
    {
        public int ProductId { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Barcode { get; set; }
        public required string Type { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }  
        public int WeightInML { get; set; }
        public bool IsActive { get; set; }


    }
}
