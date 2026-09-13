namespace Salesync.Application.Modules.DataImport.Dtos
{
    public class ImportValidationErrorDto
    {
        public int RowNumber { get; set; }

        public string? ColumnName { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}