namespace UniAcademic.Infrastructure.SeedData.Models;

public sealed class FacultySeedItem
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;
}
