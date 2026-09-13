using Salesync.Application.Modules.DataImport.Dtos;

namespace Salesync.Application.Modules.DataImport.Interfaces
{
    public interface ICustomerImportService
    {
        byte[] GenerateTemplate();

        Task<ImportPreviewDto> ValidateAsync(
            Stream fileStream,
            string fileName,
            string? userId,
            CancellationToken cancellationToken = default);

        Task<ImportResultDto> ImportAsync(
            int batchId,
            CancellationToken cancellationToken = default);

        Task<byte[]> GenerateErrorReportAsync(
            int batchId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<ImportBatchHistoryDto>> GetHistoryAsync(
            CancellationToken cancellationToken = default);
    }
}