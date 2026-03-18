namespace UniAcademic.SharedKernel;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
