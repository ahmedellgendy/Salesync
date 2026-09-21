using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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


    public CompanyProfileService(
        IUnitOfWork unitOfWork,
        IOptions<CompanySettings> companyOptions)
    {
        _unitOfWork = unitOfWork;

        _companySettings = companyOptions.Value;
    }


    public async Task<CompanyProfileDto> GetAsync()
    {
        var profile =
            await _unitOfWork.CompanyProfiles
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

        if (profile is not null)
        {
            return Map(profile);
        }

        return new CompanyProfileDto
        {
            Id = 0,

            Name =
                _companySettings.Name,

            ShortName =
                _companySettings.ShortName,

            LogoUrl =
                _companySettings.LogoUrl,

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

        Validate(dto);

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
                    Name =
                        dto.Name.Trim(),

                    ShortName =
                        dto.ShortName.Trim(),

                    LogoUrl =
                        Normalize(dto.LogoUrl),

                    Address =
                        Normalize(dto.Address),

                    Phone =
                        Normalize(dto.Phone),

                    Email =
                        Normalize(dto.Email),

                    TaxRegistrationNumber =
                        Normalize(
                            dto.TaxRegistrationNumber),

                    CommercialRegistrationNumber =
                        Normalize(
                            dto.CommercialRegistrationNumber),

                    Country =
                        dto.Country.Trim(),

                    Currency =
                        dto.Currency.Trim().ToUpperInvariant(),

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
            profile.Name =
                dto.Name.Trim();

            profile.ShortName =
                dto.ShortName.Trim();

            profile.LogoUrl =
                Normalize(dto.LogoUrl);

            profile.Address =
                Normalize(dto.Address);

            profile.Phone =
                Normalize(dto.Phone);

            profile.Email =
                Normalize(dto.Email);

            profile.TaxRegistrationNumber =
                Normalize(
                    dto.TaxRegistrationNumber);

            profile.CommercialRegistrationNumber =
                Normalize(
                    dto.CommercialRegistrationNumber);

            profile.Country =
                dto.Country.Trim();

            profile.Currency =
                dto.Currency.Trim().ToUpperInvariant();

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

        return Map(profile);
    }


    private static void Validate(
        UpdateCompanyProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException(
                "Company name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.ShortName))
        {
            throw new ArgumentException(
                "Company short name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Country))
        {
            throw new ArgumentException(
                "Country is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Currency))
        {
            throw new ArgumentException(
                "Currency is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.PrimaryColor))
        {
            throw new ArgumentException(
                "Primary color is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.SecondaryColor))
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
        CompanyProfile profile)
    {
        return new CompanyProfileDto
        {
            Id =
                profile.Id,

            Name =
                profile.Name,

            ShortName =
                profile.ShortName,

            LogoUrl =
                profile.LogoUrl,

            Address =
                profile.Address,

            Phone =
                profile.Phone,

            Email =
                profile.Email,

            TaxRegistrationNumber =
                profile.TaxRegistrationNumber,

            CommercialRegistrationNumber =
                profile.CommercialRegistrationNumber,

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