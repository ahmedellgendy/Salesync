using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.Company.Entities;

public class CompanyProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? TaxRegistrationNumber { get; set; }

    public string? CommercialRegistrationNumber { get; set; }

    public string Country { get; set; } = "Egypt";

    public string Currency { get; set; } = "EGP";

    public string PrimaryColor { get; set; } = "#0B2A5B";

    public string SecondaryColor { get; set; } = "#C9A227";
}