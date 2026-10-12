using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModFileRedirectorTests
{
    private const string GameDirectory = @"D:\Steam\steamapps\common\BBTAG";
    private const string GameFile = @"D:\Steam\steamapps\common\BBTAG\data\char\char_adh_col.pac";
    private const string ModFile = @"D:\Mods\Overhaul\data2\char\char_adh_col.pac";
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileWriteData = 0x00000002;
    private const uint FileAppendData = 0x00000004;
    private const uint CreateAlways = 2;
    private const uint OpenAlways = 4;

    private readonly ModFileRedirector _redirector;

    public ModFileRedirectorTests()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.EnumerateFilesRecursively(@"D:\Mods\Overhaul\data2").Returns([ModFile]);
        var mod = new ModFolder("Overhaul", @"D:\Mods\Overhaul", [new ModDataRoot(@"D:\Mods\Overhaul\data2", "data")], null);
        _redirector = new ModFileRedirector(ModFileIndex.Build([mod], fileSystem), GameDirectory);
    }

    [Fact]
    public void RedirectOpen_SendsReadOnlyOpenToModFile()
    {
        Assert.Equal(ModFile, _redirector.RedirectOpen(GameFile, GenericRead, ModFileRedirector.OpenExisting));
        Assert.Equal(ModFile, _redirector.RedirectOpen("data/char/char_adh_col.pac", GenericRead, ModFileRedirector.OpenExisting));
    }

    [Theory]
    [InlineData(GenericRead | GenericWrite, ModFileRedirector.OpenExisting)]
    [InlineData(FileWriteData, ModFileRedirector.OpenExisting)]
    [InlineData(FileAppendData, ModFileRedirector.OpenExisting)]
    [InlineData(GenericRead, CreateAlways)]
    [InlineData(GenericRead, OpenAlways)]
    public void RedirectOpen_LeavesWritesToTheGame(uint access, uint disposition)
    {
        Assert.Null(_redirector.RedirectOpen(GameFile, access, disposition));
    }

    [Fact]
    public void RedirectOpen_LeavesFilesNoModProvides()
    {
        Assert.Null(_redirector.RedirectOpen(@"D:\Steam\steamapps\common\BBTAG\data\char\char_adh_pal.pac", GenericRead, ModFileRedirector.OpenExisting));
        Assert.Null(_redirector.RedirectOpen(null, GenericRead, ModFileRedirector.OpenExisting));
    }

    [Fact]
    public void RedirectLookup_SendsAttributeQueriesToModFile()
    {
        Assert.Equal(ModFile, _redirector.RedirectLookup(GameFile));
        Assert.Null(_redirector.RedirectLookup(@"D:\Steam\steamapps\common\BBTAG\data\char\*.pac"));
    }

    [Fact]
    public void Redirections_CountsEveryRedirect()
    {
        _redirector.RedirectOpen(GameFile, GenericRead, ModFileRedirector.OpenExisting);
        _redirector.RedirectLookup(GameFile);
        _redirector.RedirectLookup(@"D:\Elsewhere\file.pac");

        Assert.Equal(2, _redirector.Redirections);
    }
}
