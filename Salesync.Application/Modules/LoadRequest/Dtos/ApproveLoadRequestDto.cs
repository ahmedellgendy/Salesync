namespace Salesync.Application.Modules.LoadRequest.Dtos
{
    public class ApproveLoadRequestDto
    {
        public string? Notes { get; set; }

        public List<ApproveLoadRequestItemDto> Items { get; set; } = new();
    }
}