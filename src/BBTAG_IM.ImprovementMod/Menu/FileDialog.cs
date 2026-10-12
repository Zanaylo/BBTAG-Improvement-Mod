using System.Runtime.CompilerServices;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Menu;

internal enum FileDialogPurpose
{
    Import,
    Export,
}

internal sealed record FileDialogAnswer(FileDialogPurpose Purpose, IReadOnlyList<string> Paths);

internal sealed unsafe class FileDialog
{
    private const int ManyFilesLength = 64 * 1024;
    private const int OneFileLength = 1024;
    private const string Filter = "Palettes (*.impl)\0*.impl\0\0";
    private const string Extension = "impl";
    private const string ThreadName = "Palette file dialog";
    private const int Closed = 0;
    private const int Opened = 1;

    private FileDialogAnswer? _answer;
    private int _state;

    public bool IsOpen => Volatile.Read(ref _state) == Opened;

    public void OpenPalettes() => Start(FileDialogPurpose.Import, "Import palettes", "");

    public void SavePalette(string fileName) => Start(FileDialogPurpose.Export, "Export palette", fileName);

    public FileDialogAnswer? Take() => Interlocked.Exchange(ref _answer, null);

    private void Start(FileDialogPurpose purpose, string title, string fileName)
    {
        if (Interlocked.Exchange(ref _state, Opened) == Opened)
            return;

        var thread = new Thread(() => Run(purpose, title, fileName)) { IsBackground = true, Name = ThreadName };

        if (OperatingSystem.IsWindows())
            thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
    }

    private void Run(FileDialogPurpose purpose, string title, string fileName)
    {
        try
        {
            var paths = purpose == FileDialogPurpose.Import ? AskForFiles(title) : AskForNewFile(title, fileName);

            if (paths.Count > 0)
                Volatile.Write(ref _answer, new FileDialogAnswer(purpose, paths));
        }
        finally
        {
            Volatile.Write(ref _state, Closed);
        }
    }

    private static IReadOnlyList<string> AskForFiles(string title)
    {
        var buffer = new char[ManyFilesLength];
        var flags = Comdlg32.PathMustExist | Comdlg32.FileMustExist | Comdlg32.KeepWorkingDirectory | Comdlg32.AllowMultipleFiles | Comdlg32.Explorer;

        return Ask(buffer, title, flags, Comdlg32.GetOpenFileName) ? OpenFileSelection.Paths(new string(buffer)) : [];
    }

    private static IReadOnlyList<string> AskForNewFile(string title, string fileName)
    {
        var buffer = new char[OneFileLength];
        fileName.AsSpan(0, Math.Min(fileName.Length, OneFileLength - 1)).CopyTo(buffer);
        var flags = Comdlg32.PathMustExist | Comdlg32.KeepWorkingDirectory | Comdlg32.OverwritePrompt;

        if (!Ask(buffer, title, flags, Comdlg32.GetSaveFileName))
            return [];

        return [new string(buffer, 0, Array.IndexOf(buffer, '\0'))];
    }

    private static bool Ask(char[] buffer, string title, int flags, DialogCall show)
    {
        var workingDirectory = Environment.CurrentDirectory;

        fixed (char* file = buffer, filter = Filter, caption = title, extension = Extension)
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
