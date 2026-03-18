namespace UniAcademic.SharedKernel;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }

    string? CreatedBy { get; set; }

    DateTime? ModifiedAtUtc { get; set; }

    string? ModifiedBy { get; set; }
}
