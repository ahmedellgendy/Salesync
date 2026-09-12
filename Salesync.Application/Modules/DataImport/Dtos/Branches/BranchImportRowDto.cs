namespace Salesync.Application.Modules.DataImport.Dtos.Branches
{
    public class BranchImportRowDto
    {
        public int RowNumber { get; set; }

        public string BranchCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }
    }
}