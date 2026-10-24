using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Salesync.Application.Common.Licensing;
using Salesync.Application.Common.Settings;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Company.Dtos;
using Salesync.Application.Modules.Company.Interfaces;
using Salesync.Domain.Modules.Company.Entities;

namespace Salesync.Application.Modules.Company.Services;

public class CompanyProfileService : ICompanyProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CompanySettings _companySettings;
    private readonly ILicenseService _licenseService;


    public CompanyProfileService(
        IUnitOfWork unitOfWork,
        IOptions<CompanySettings> companyOptions,
        ILicenseService licenseService)
    {
        _unitOfWork =
            unitOfWork;

        _companySettings =
            companyOptions.Value;

        _licenseService =
            licenseService;
    }


    public async Task<CompanyProfileDto> GetAsync()
    {
        var license =
            _licenseService.GetStatus();

        EnsureCompanyIdentityIsAvailable(
            license);


        var profile =
            await _unitOfWork.CompanyProfiles
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();


        if (profile is not null)
        {
            return Map(
                profile,
                license);
        }


        return new CompanyProfileDto
        {
            Id = 0,

            CompanyId =
                license.CompanyId,

            Name =
                license.CompanyName,

            ShortName =
                license.ShortName,

            TaxRegistrationNumber =
                license.TaxRegistrationNumber,

            CommercialRegistrationNumber =
                license.CommercialRegistrationNumber,

            LogoUrl =
                _companySettings.LogoUrl,

            Address =
                null,

            Phone =
                null,

            Email =
                null,

            Country =
                string.IsNullOrWhiteSpace(
                    _companySettings.Country)
                    ? "Egypt"
                    : _companySettings.Country,

            Currency =
                string.IsNullOrWhiteSpace(
                    _companySettings.Currency)
                    ? "EGP"
                    : _companySettings.Currency,

            PrimaryColor =
                string.IsNullOrWhiteSpace(
                    _companySettings.PrimaryColor)
                    ? "#0B2A5B"
                    : _companySettings.PrimaryColor,

            SecondaryColor =
                string.IsNullOrWhiteSpace(
                    _companySettings.SecondaryColor)
                    ? "#C9A227"
                    : _companySettings.SecondaryColor
        };
    }


    public async Task<CompanyProfileDto> UpdateAsync(
        UpdateCompanyProfileDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);


        var license =
            _licenseService.GetStatus();

        EnsureCompanyIdentityIsAvailable(
            license);


        ValidateProfileSettings(dto);


        var profile =
            await _unitOfWork.CompanyProfiles
                .GetQueryable()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();


        if (profile is null)
        {
            profile =
                new CompanyProfile
                {
                    //
                    // Company identity is controlled
                    // by the signed Salesync license.
                    //
                    Name =
                        license.CompanyName,

                    ShortName =
                        license.ShortName,

                    TaxRegistrationNumber =
                        Normalize(
                            license.TaxRegistrationNumber),

                    CommercialRegistrationNumber =
                        Normalize(
                            license.CommercialRegistrationNumber),

                    //
                    // Company profile / branding data.
                    //
                    LogoUrl =
                        Normalize(dto.LogoUrl),

                    Address =
                        Normalize(dto.Address),

                    Phone =
                        Normalize(dto.Phone),

                    Email =
                        Normalize(dto.Email),

                    Country =
                        dto.Country.Trim(),

                    Currency =
                        dto.Currency
                            .Trim()
                            .ToUpperInvariant(),

                    PrimaryColor =
                        dto.PrimaryColor.Trim(),

                    SecondaryColor =
                        dto.SecondaryColor.Trim()
                };


            await _unitOfWork.CompanyProfiles
                .AddAsync(profile);
        }
        else
        {
            //
            // Always synchronize protected identity
            // fields from the license.
            //
            profile.Name =
                license.CompanyName;

            profile.ShortName =
                license.ShortName;

            profile.TaxRegistrationNumber =
                Normalize(
                    license.TaxRegistrationNumber);

            profile.CommercialRegistrationNumber =
                Normalize(
                    license.CommercialRegistrationNumber);


            //
            // Only profile / branding fields
            // come from the update request.
            //
            profile.LogoUrl =
                Normalize(dto.LogoUrl);

            profile.Address =
                Normalize(dto.Address);

            profile.Phone =
                Normalize(dto.Phone);

            profile.Email =
                Normalize(dto.Email);

            profile.Country =
                dto.Country.Trim();

            profile.Currency =
                dto.Currency
                    .Trim()
                    .ToUpperInvariant();

            profile.PrimaryColor =
                dto.PrimaryColor.Trim();

            profile.SecondaryColor =
                dto.SecondaryColor.Trim();

            profile.UpdatedAt =
                DateTime.UtcNow;


            _unitOfWork.CompanyProfiles
                .Update(profile);
        }


        await _unitOfWork.CompleteAsync();


        return Map(
            profile,
            license);
    }


    private static void EnsureCompanyIdentityIsAvailable(
        LicenseStatus license)
    {
        if (!license.IsConfigured)
        {
            throw new InvalidOperationException(
                "Salesync license is not configured.");
        }


        if (!license.IsValid)
        {
            throw new InvalidOperationException(
                license.Message
                ?? "Salesync license is invalid.");
        }


        if (string.IsNullOrWhiteSpace(
                license.CompanyId))
        {
            throw new InvalidOperationException(
                "License company id is missing.");
        }


        if (string.IsNullOrWhiteSpace(
                license.CompanyName))
        {
            throw new InvalidOperationException(
                "License company name is missing.");
        }


        if (string.IsNullOrWhiteSpace(
                license.ShortName))
        {
            throw new InvalidOperationException(
                "License company short name is missing.");
        }
    }


    private static void ValidateProfileSettings(
        UpdateCompanyProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(
                dto.Country))
        {
            throw new ArgumentException(
                "Country is required.");
        }


        if (string.IsNullOrWhiteSpace(
                dto.Currency))
        {
            throw new ArgumentException(
                "Currency is required.");
        }


        if (string.IsNullOrWhiteSpace(
                dto.PrimaryColor))
        {
            throw new ArgumentException(
                "Primary color is required.");
        }


        if (string.IsNullOrWhiteSpace(
                dto.SecondaryColor))
        {
            throw new ArgumentException(
                "Secondary color is required.");
        }
    }


    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }


    private static CompanyProfileDto Map(
        CompanyProfile profile,
        LicenseStatus license)
    {
        return new CompanyProfileDto
        {
            Id =
                profile.Id,

            CompanyId =
                license.CompanyId,

            //
            // Always return company identity
            // from the signed license.
            //
            Name =
                license.CompanyName,

            ShortName =
                license.ShortName,

            TaxRegistrationNumber =
                license.TaxRegistrationNumber,

            CommercialRegistrationNumber =
                license.CommercialRegistrationNumber,

            //
            // Branding / contact data
            // comes from CompanyProfile.
            //
            LogoUrl =
                profile.LogoUrl,

            Address =
                profile.Address,

            Phone =
                profile.Phone,

            Email =
                profile.Email,

            Country =
                profile.Country,

            Currency =
                profile.Currency,

            PrimaryColor =
                profile.PrimaryColor,

            SecondaryColor =
                profile.SecondaryColor
        };
    }
}