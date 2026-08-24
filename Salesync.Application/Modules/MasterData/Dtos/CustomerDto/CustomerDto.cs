using Salesync.Domain.Common.Enums.MasterData;

namespace Salesync.Application.Modules.MasterData.Dtos.CustomerDto
{
    public class CustomerDto
    {
        public int Id { get; set; }

        // Basic Info
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // Address
        public string? Country { get; set; }
        public string? Address { get; set; }
        public string? Area { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }

        // GPS
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Classification
        public string? CategoryCode { get; set; }
        public string? SalesSectorCode { get; set; }
        public string? ClassId { get; set; }
        public CustomerType Type { get; set; }

        // Payment
        public bool AllowCash { get; set; }
        public bool AllowCheck { get; set; }
        public bool AllowCreditCard { get; set; }
        public string? PaymentTermsCode { get; set; }

        // Credit
        public decimal CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool ForceCreditLimit { get; set; }
        public decimal OrderCeiling { get; set; }

        // Business Rules
        public bool ReturnWithoutInvoice { get; set; }
        public bool MandatoryPhoto { get; set; }

        // Head Office
        public bool IsHeadOffice { get; set; }
        public int? HeadOfficeId { get; set; }
        public string? HeadOfficeName { get; set; }

        // Tracking
        public decimal TotalPurchaseAmount { get; set; }
        public DateTime? LastPurchaseDate { get; set; }

        public CustomerStatus Status { get; set; }

        // External References
        public string? AccountNumber { get; set; }
        public string? TaxId { get; set; }
        public string? PriceId { get; set; }

        // Branch
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}