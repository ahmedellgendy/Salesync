using Salesync.Domain.Modules.DataImport.Enums;

namespace Salesync.Application.Modules.DataImport.Dtos
{
    public class ImportPreviewDto
    {
        public int BatchId { get; set; }

        public ImportType ImportType { get; set; }

        public string FileName { get; set; } = string.Empty;

        public int TotalRows { get; set; }

        public int ValidRows { get; set; }

        public int ErrorRows { get; set; }

        public bool CanImport =>
            TotalRows > 0 &&
            ErrorRows == 0 &&
            ValidRows == TotalRows;

        public List<ImportValidationErrorDto> Errors { get; set; }
            = new();
    }
}