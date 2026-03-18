using UniAcademic.Application.Abstractions.Common;

namespace UniAcademic.Infrastructure.Services.Common;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
