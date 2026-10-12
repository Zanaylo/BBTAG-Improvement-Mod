using BBTAG_IM.Core.Tables;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class ScriptNode : ObservableObject
{
    private const string AlwaysOnSuffix = " (always on)";

    private bool _isChecked;

    public ScriptNode(int id, string description, bool canToggle, bool isRequired, bool isExpanded, IReadOnlyList<ScriptNode> children)
    {
        Id = id;
        Label = isRequired ? description + AlwaysOnSuffix : description;
        CanToggle = canToggle;
        IsRequired = isRequired;
        IsExpanded = isExpanded;
        Children = children;
    }

    public event Action<ScriptNode>? Toggled;

    public int Id { get; }

    public string Label { get; }

    public bool CanToggle { get; }

    public bool IsRequired { get; }

    public bool IsExpanded { get; set; }

    public IReadOnlyList<ScriptNode> Children { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value))
                Toggled?.Invoke(this);
        }
    }

    public void Show(bool isChecked)
    {
        if (_isChecked == isChecked)
            return;

        _isChecked = isChecked;
        OnPropertyChanged(nameof(IsChecked));
    }

    public IEnumerable<ScriptNode> SelfAndDescendants() => Children.SelectMany(child => child.SelfAndDescendants()).Prepend(this);

    public static IReadOnlyList<ScriptNode> From(CheatTable table) =>
        table.Scripts.Select(script => From(script, table, isTopLevel: true)).ToArray();

    private static ScriptNode From(TableScript script, CheatTable table, bool isTopLevel) =>
        new(
            script.Id,
            script.Description,
            script.CanToggle,
            table.IsRequired(script.Id),
            isTopLevel,
            script.Children.Select(child => From(child, table, isTopLevel: false)).ToArray());
}
