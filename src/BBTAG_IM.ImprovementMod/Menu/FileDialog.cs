using System.Runtime.CompilerServices;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Menu;

internal enum FileDialogPurpose
{
    Import,
    Export,
    Program,
    Game,
}

internal sealed record FileDialogAnswer(FileDialogPurpose Purpose, IReadOnlyList<string> Paths);

internal sealed unsafe class FileDialog
{
    private const int ManyFilesLength = 64 * 1024;
    private const int OneFileLength = 1024;
    private const string PaletteFilter = "Palettes (*.impl)\0*.impl\0\0";
    private const string PaletteExtension = "impl";
    private const string ProgramFilter = "Programs (*.exe)\0*.exe\0\0";
    private const string ProgramExtension = "exe";
    private const string ThreadName = "File dialog";
    private const int Closed = 0;
    private const int Opened = 1;

    private FileDialogAnswer? _answer;
    private int _state;

    public bool IsOpen => Volatile.Read(ref _state) == Opened;

    public void OpenPalettes() => Start(FileDialogPurpose.Import, "Import palettes", "");

    public void SavePalette(string fileName) => Start(FileDialogPurpose.Export, "Export palette", fileName);

    public void ChooseProgram() => Start(FileDialogPurpose.Program, "Choose the program that opens scripts", "");

    public void ChooseGame(string title, string filter) => Start(FileDialogPurpose.Game, title, "", filter);

    public FileDialogAnswer? Take() => Interlocked.Exchange(ref _answer, null);

    private void Start(FileDialogPurpose purpose, string title, string fileName, string filter = "")
    {
        if (Interlocked.Exchange(ref _state, Opened) == Opened)
            return;

        var thread = new Thread(() => Run(purpose, title, fileName, filter)) { IsBackground = true, Name = ThreadName };

        if (OperatingSystem.IsWindows())
            thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
    }

    private void Run(FileDialogPurpose purpose, string title, string fileName, string filter)
    {
        try
        {
            var paths = purpose switch
            {
                FileDialogPurpose.Import => AskForFiles(title, PaletteFilter, PaletteExtension, Comdlg32.AllowMultipleFiles),
                FileDialogPurpose.Program => AskForFiles(title, ProgramFilter, ProgramExtension, 0),
                FileDialogPurpose.Game => AskForFiles(title, filter, ProgramExtension, 0),
                _ => AskForNewFile(title, fileName),
            };

            if (paths.Count > 0)
                Volatile.Write(ref _answer, new FileDialogAnswer(purpose, paths));
        }
        finally
        {
            Volatile.Write(ref _state, Closed);
        }
    }

    private static IReadOnlyList<string> AskForFiles(string title, string filter, string extension, int extraFlags)
    {
        var buffer = new char[ManyFilesLength];
        var flags = Comdlg32.PathMustExist | Comdlg32.FileMustExist | Comdlg32.KeepWorkingDirectory | Comdlg32.Explorer | extraFlags;

        return Ask(buffer, title, filter, extension, flags, Comdlg32.GetOpenFileName) ? OpenFileSelection.Paths(new string(buffer)) : [];
    }

    private static IReadOnlyList<string> AskForNewFile(string title, string fileName)
    {
        var buffer = new char[OneFileLength];
        fileName.AsSpan(0, Math.Min(fileName.Length, OneFileLength - 1)).CopyTo(buffer);
        var flags = Comdlg32.PathMustExist | Comdlg32.KeepWorkingDirectory | Comdlg32.OverwritePrompt;

        if (!Ask(buffer, title, PaletteFilter, PaletteExtension, flags, Comdlg32.GetSaveFileName))
            return [];

        return [new string(buffer, 0, Array.IndexOf(buffer, '\0'))];
    }

    private static bool Ask(char[] buffer, string title, string filterText, string extensionText, int flags, DialogCall show)
    {
        var workingDirectory = Environment.CurrentDirectory;

        fixed (char* file = buffer, filter = filterText, caption = title, extension = extensionText)
        {
            var dialog = new OpenFileName
            {
                StructSize = Unsafe.SizeOf<OpenFileName>(),
                Filter = (nint)filter,
                File = (nint)file,
                FileLength = buffer.Length,
                Title = (nint)caption,
                DefaultExtension = (nint)extension,
                Flags = flags,
            };

            var accepted = show(ref dialog);
            Environment.CurrentDirectory = workingDirectory;
            return accepted;
        }
    }

    private delegate bool DialogCall(ref OpenFileName dialog);
}
