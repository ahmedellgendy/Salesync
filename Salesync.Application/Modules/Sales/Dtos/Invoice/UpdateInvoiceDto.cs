using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.Invoice
{
    public class UpdateInvoiceDto
    {
        public InvoiceStatus? Status { get; set; }
        public string? Notes { get; set; }
    }
}
