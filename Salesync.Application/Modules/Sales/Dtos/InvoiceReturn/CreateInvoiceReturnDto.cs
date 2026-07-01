using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn
{
    public class CreateInvoiceReturnDto
    {
        public int InvoiceId { get; set; }
        public int? SalesRepId { get; set; }
        public ReturnReason ReturnReason { get; set; }
        public string? ReasonNotes { get; set; }
        public List<CreateInvoiceReturnItemDto> Items { get; set; } = new();
    }
}
