namespace Salesync.Application.Modules.DataImport.Interfaces
{
    public interface IExcelImportService
    {
        Task<IReadOnlyList<Dictionary<string, string>>> ReadAsync(
            Stream stream,
            CancellationToken cancellationToken = default);

        byte[] GenerateTemplate(
            string sheetName,
            IReadOnlyList<string> headers);

        byte[] GenerateErrorReport(
            string sheetName,
            IReadOnlyList<string> headers,
            IReadOnlyList<Dictionary<string, string>> rows,
            IReadOnlyDictionary<int, List<string>> rowErrors);
    }
}