using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Identity;

public sealed class Permission : AuditableEntity, IAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
