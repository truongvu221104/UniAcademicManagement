namespace UniAcademic.Application.Models.Materials;

public sealed class SetCourseMaterialPublishStateCommand
{
    public Guid Id { get; set; }

    public bool IsPublished { get; set; }
}
