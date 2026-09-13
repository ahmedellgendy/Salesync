using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.DataImport.Entities
{
    public class ImportBatchRow : BaseEntity
    {
        public int ImportBatchId { get; set; }

        public int RowNumber { get; set; }

        public string DataJson { get; set; } = string.Empty;

        public bool IsValid { get; set; }

        public string? ErrorsJson { get; set; }

        public ImportBatch? ImportBatch { get; set; }
    }
}