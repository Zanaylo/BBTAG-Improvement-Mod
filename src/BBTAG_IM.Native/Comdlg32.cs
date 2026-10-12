using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

[StructLayout(LayoutKind.Sequential)]
public struct OpenFileName
{
    public int StructSize;
    public nint Owner;
    public nint Instance;
    public nint Filter;
    public nint CustomFilter;
    public int CustomFilterLength;
    public int FilterIndex;
    public nint File;
    public int FileLength;
    public nint FileTitle;
    public int FileTitleLength;
    public nint InitialDirectory;
    public nint Title;
    public int Flags;
    public short FileOffset;
    public short FileExtension;
    public nint DefaultExtension;
    public nint CustomData;
    public nint Hook;
    public nint TemplateName;
    public nint Reserved;
    public int ReservedNumber;
    public int FlagsEx;
}

public static partial class Comdlg32
{
    public const int OverwritePrompt = 0x2;
    public const int KeepWorkingDirectory = 0x8;
    public const int AllowMultipleFiles = 0x200;
    public const int PathMustExist = 0x800;
    public const int FileMustExist = 0x1000;
    public const int Explorer = 0x80000;

    private const string Library = "comdlg32.dll";

    [LibraryImport(Library, EntryPoint = "GetOpenFileNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetOpenFileName(ref OpenFileName dialog);

    [LibraryImport(Library, EntryPoint = "GetSaveFileNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetSaveFileName(ref OpenFileName dialog);
}
