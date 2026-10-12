using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteFiles(PaletteLibrary library, MenuSettings settings, FileDialog dialog, PaletteImport import)
{
    private ImplPalette? _exporting;

    public bool IsDialogOpen => dialog.IsOpen;

    public void Import() => dialog.OpenPalettes();

    public void Export(ImplPalette palette)
    {
        if (dialog.IsOpen)
            return;

        _exporting = palette;
        dialog.SavePalette(palette.Name + ImplPalette.Extension);
    }

    public void Draw()
    {
        TakeAnswer();
        import.Draw();
    }

    private void TakeAnswer()
    {
        if (dialog.Take() is not { } answer)
            return;

        if (answer.Purpose == FileDialogPurpose.Import)
        {
            import.Files(answer.Paths);
            return;
        }

        FinishExport(answer.Paths[0]);
    }

    private void FinishExport(string path)
    {
        if (_exporting is not { } palette)
            return;

        _exporting = null;

        try
        {
            library.Export(palette, path);
            settings.Report($"Exported {palette.Name} to {path}.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            settings.Report($"{palette.Name} could not be exported: {exception.Message}");
        }
    }
}
