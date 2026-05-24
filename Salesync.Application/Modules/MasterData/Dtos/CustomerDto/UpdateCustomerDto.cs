using Salesync.Domain.Common.Enums;

namespace Salesync.Application.Modules.MasterData.Dtos.CustomerDto
{
    public class UpdateCustomerDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public string? Country { get; set; }
        public string? Address { get; set; }
        public string? Area { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string? CategoryCode { get; set; }
        public string? SalesSectorCode { get; set; }
        public string? ClassId { get; set; }
        public CustomerType Type { get; set; }

        public bool AllowCash { get; set; }
        public bool AllowCheck { get; set; }
        public bool AllowCreditCard { get; set; }
        public string? PaymentTermsCode { get; set; }

        public decimal CreditLimit { get; set; }
        public decimal OrderCeiling { get; set; }

        public string? AccountNumber { get; set; }
        public string? TaxId { get; set; }
        public string? PriceId { get; set; }

        public int? BranchId { get; set; }

        public CustomerStatus Status { get; set; }
    }
}
