namespace Salesync.Application.Modules.LoadRequest.Dtos
{
    public class ConfirmLoadRequestDto
    {
        public string? Notes { get; set; }

        public List<ConfirmLoadRequestItemDto> Items { get; set; } = new();
    }
}