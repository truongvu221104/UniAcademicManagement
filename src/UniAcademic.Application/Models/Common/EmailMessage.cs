namespace UniAcademic.Application.Models.Common;

public sealed class EmailMessage
{
    public string ToEmail { get; set; } = string.Empty;

    public string? ToName { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string PlainTextBody { get; set; } = string.Empty;

    public string? HtmlBody { get; set; }
}
