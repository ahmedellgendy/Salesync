using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn
{
    public class InvoiceReturnDto
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = null!;
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public ReturnStatus Status { get; set; }
        public ReturnReason ReturnReason { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ReasonNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
