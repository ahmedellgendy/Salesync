namespace Salesync.Application.Modules.DataImport.Dtos
{
    public class ImportResultDto
    {
        public int BatchId { get; set; }

        public int ImportedRows { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}