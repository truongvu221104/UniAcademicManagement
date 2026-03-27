using UniAcademic.Application.Models.Common;

namespace UniAcademic.Application.Abstractions.Common;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
