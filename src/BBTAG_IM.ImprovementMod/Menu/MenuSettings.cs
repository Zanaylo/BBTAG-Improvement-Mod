using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class MenuSettings(SettingsWatcher<ImprovementSettings> settings)
{
    public ImprovementSettings Current => settings.Current;

    public string Status { get; private set; } = "";

    public void Report(string message) => Status = message;

    public void ClearStatus() => Status = "";

    public void Save(Action<ImprovementSettings> change, string? confirmation = null)
    {
        try
        {
            settings.Change(change);

            if (confirmation is not null)
                Status = confirmation;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Status = $"The choice could not be saved: {exception.Message}";
        }
    }
}
