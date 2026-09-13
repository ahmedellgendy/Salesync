using Salesync.Domain.Modules.DataImport.Enums;

namespace Salesync.Application.Modules.DataImport.Dtos
{
    public class ImportBatchHistoryDto
    {
        public int Id { get; set; }

        public ImportType ImportType { get; set; }

        public ImportStatus Status { get; set; }

        public string FileName { get; set; } = string.Empty;

        public int TotalRows { get; set; }

        public int ValidRows { get; set; }

        public int ErrorRows { get; set; }

        public int ImportedRows { get; set; }

        public string? ImportedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}