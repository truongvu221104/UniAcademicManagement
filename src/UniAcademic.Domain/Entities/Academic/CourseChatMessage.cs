using UniAcademic.Domain.Entities.Identity;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class CourseChatMessage : AuditableEntity, IAuditableEntity
{
    public Guid CourseOfferingId { get; set; }

    public Guid SenderUserId { get; set; }

    public string SenderUsername { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = string.Empty;

    public string SenderRole { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public CourseOffering? CourseOffering { get; set; }

    public User? SenderUser { get; set; }
}
