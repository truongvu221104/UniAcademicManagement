namespace UniAcademic.AdminApp.Navigation;

public sealed class ModuleDefinition
{
    public required string Group { get; init; }

    public required string Title { get; init; }

    public required Func<ViewModels.ModulePageViewModel> Create { get; init; }
}
