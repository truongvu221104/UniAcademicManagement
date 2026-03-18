using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Identity;

public sealed class Role : AuditableEntity, IAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
