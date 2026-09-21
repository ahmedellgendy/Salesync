using Salesync.Application.Modules.Company.Dtos;

namespace Salesync.Application.Modules.Company.Interfaces;

public interface ICompanyProfileService
{
    Task<CompanyProfileDto> GetAsync();

    Task<CompanyProfileDto> UpdateAsync(
        UpdateCompanyProfileDto dto);
}