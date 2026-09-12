namespace Salesync.Application.Modules.DataImport.Dtos.Customers
{
    public class CustomerImportRowDto
    {
        public int RowNumber { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Country { get; set; }

        public string Address { get; set; } = string.Empty;

        public string? Area { get; set; }

        public string? City { get; set; }

        public string? District { get; set; }

        public string? Region { get; set; }

        public string? PostalCode { get; set; }

        public string? Latitude { get; set; }

        public string? Longitude { get; set; }

        public string? CategoryCode { get; set; }

        public string? SalesSectorCode { get; set; }

        public string? ClassId { get; set; }

        public string CustomerType { get; set; } = "Individual";

        public string AllowCash { get; set; } = "true";

        public string AllowCheck { get; set; } = "false";

        public string AllowCreditCard { get; set; } = "false";

        public string? PaymentTermsCode { get; set; }

        public string? CreditLimit { get; set; }

        public string? OrderCeiling { get; set; }

        public string? AccountNumber { get; set; }

        public string? TaxId { get; set; }

        public string? PriceId { get; set; }

        public string? BranchCode { get; set; }
    }
}