using Salesync.Domain.Common;
using Salesync.Domain.Modules.DataImport.Enums;

namespace Salesync.Domain.Modules.DataImport.Entities
{
    public class ImportBatch : BaseEntity
    {
        public ImportType ImportType { get; set; }

        public ImportStatus Status { get; set; }

        public required string FileName { get; set; }

        public int TotalRows { get; set; }

        public int ValidRows { get; set; }

        public int ErrorRows { get; set; }

        public int ImportedRows { get; set; }

        public string? ErrorReportFileName { get; set; }

        public string? Notes { get; set; }

        public string? ImportedByUserId { get; set; }

        public DateTime? ValidatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
        public ICollection<ImportBatchRow> Rows { get; set; }
    = new List<ImportBatchRow>();
    }
}