using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Palettes;

public class PaletteEditorTests
{
    private const uint GameObject = 0x00E59100;
    private const uint BattleObject = 0x014A77A0;
    private const uint CharacterList = 0x0157D3F8;
    private const uint CameraList = 0x00C0A788;
    private const uint FirstCharacter = 0x09000000;
    private const uint CharacterStride = 0x30000;
    private const uint FirstTableArea = 0x20000000;
    private const uint TableAreaStride = 0x01000000;
    private const uint RosterGetter = 0x00671E00;
    private const uint Roster = 0x00C37768;
    private const uint RosterRecordLength = 0xB8;
    private const string ModFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string AegisFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\palettes\Aegis";
    private const string KohakuFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\palettes\mko";
    private const int Aegis = 24;
    private const int Kohaku = 120;
    private const int Match = 11;
    private const int CharacterSelect = 6;
    private const int PlayerOneFirst = 0;
    private const int PlayerTwoFirst = 1;
    private const int CharacterFile = 0;
    private const int Entry = 9;

    private static readonly GameAddresses Addresses = new(GameObject, BattleObject, CharacterList, CameraList);
    private static readonly PaletteColor Red = new(255, 0, 0, 255);
    private static readonly PaletteColor Blue = new(0, 0, 255, 255);
    private static readonly PaletteColor Magenta = new(255, 0, 255, 255);
    private static readonly PaletteColor Cyan = new(0, 255, 255, 255);

    private readonly FakeGameMemory _memoryMock = new();
    private readonly ImprovementSettings _settings = new();

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly ILog _logMock = Substitute.For<ILog>();
    private readonly Func<int, int> _pickBelowMock = Substitute.For<Func<int, int>>();

    public PaletteEditorTests()
    {
        _memoryMock.Map(GameObject, 0x3000).Map(CharacterList, 16).Map(BattleObject, 0xA5000);
        _fileSystemMock.DirectoryExists(AegisFolder).Returns(true);
        _fileSystemMock.EnumerateFilesRecursively(AegisFolder).Returns([AegisFolder + @"\Minato.impl"]);
        _fileSystemMock.ReadAllBytes(AegisFolder + @"\Minato.impl").Returns(ImplFile.Create(Aegis, fill: 0x42));
    }

    public PaletteEditor CreatePaletteEditor() =>
        new(Game, new CharacterRoster(_memoryMock, RosterGetter), () => _settings, new PaletteCatalog(new PaletteLibrary(_fileSystemMock, ModFolder), work => work()), _logMock, _pickBelowMock);

    private GameView Game => new(_memoryMock, Addresses);

    [Fact]
    public void Update_MatchWithAChosenPalette_WritesItIntoTheChosenColor()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.All(FileBytes(PlayerOneFirst, 2), value => Assert.Equal(0x42, value));
    }

    [Fact]
    public void Update_MatchWithAChosenPalette_SwitchesToThePreviousColorSoTheGameRedrawsIt()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.Equal(1, PaletteIndex(PlayerOneFirst));
        Assert.All(FileBytes(PlayerOneFirst, 1), value => Assert.Equal(0x42, value));
    }

    [Fact]
    public void Update_FirstColorChosen_RedrawsThroughTheSecondColor()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 0);
        _settings.ChoosePalette("Aegis", 1, "Minato");
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.Equal(1, PaletteIndex(PlayerOneFirst));
    }

    [Fact]
    public void Update_ColorWithoutAChoice_LeavesTheCharacterAlone()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 7, "Minato");
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.Equal(2, PaletteIndex(PlayerOneFirst));
    }

    [Fact]
    public void Update_ChosenPaletteMissingFromTheFolder_LogsItAndLeavesTheColor()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, "Gold");
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.Equal(2, PaletteIndex(PlayerOneFirst));
        _logMock.Received(1).Info("Aegis color 3 should use the palette Gold, but it is not in the palettes folder.");
    }

    [Fact]
    public void Update_PaletteApplied_LogsWhoGotWhichPalette()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerTwoFirst, Aegis, color: 2);
        CharacterInSlot(PlayerOneFirst, Aegis, color: 5);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        _logMock.Received(1).Info("Player 2 Aegis color 3: palette Minato.");
    }

    [Fact]
    public void Update_CharacterStillLoading_WaitsForIt()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2, loaded: false);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);
        editor.Update();
        PaletteMemory.Lay(_memoryMock, Game.PaletteTableAddress(PlayerOneFirst), TableArea(PlayerOneFirst), 1, 2, 3);

        // Act
        editor.Update();

        // Assert
        Assert.Equal(1, PaletteIndex(PlayerOneFirst));
    }

    [Fact]
    public void Update_SameMatchNextFrame_DoesNotSwitchAgain()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);
        editor.Update();

        // Act
        editor.Update();

        // Assert
        Assert.Equal(1, PaletteIndex(PlayerOneFirst));
    }

    [Fact]
    public void Update_MatchOver_PutsTheChosenColorBack()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);
        editor.Update();
        GameIs(CharacterSelect);

        // Act
        editor.Update();

        // Assert
        Assert.Equal(2, PaletteIndex(PlayerOneFirst));
        Assert.Null(editor.Character(PlayerOneFirst));
    }

    [Fact]
    public void Update_MatchOverAfterThePlayerPickedAnotherColor_KeepsThePlayerChoice()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);
        editor.Update();
        GameIs(CharacterSelect);
        _memoryMock.Int32At(Game.PaletteIndexAddress(PlayerOneFirst), 9);

        // Act
        editor.Update();

        // Assert
        Assert.Equal(9, PaletteIndex(PlayerOneFirst));
    }

    [Fact]
    public void Update_NextMatch_AppliesThePalettesAgain()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);
        editor.Update();
        GameIs(CharacterSelect);
        editor.Update();
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.Equal(1, PaletteIndex(PlayerOneFirst));
    }

    [Fact]
    public void Update_NextMatchWithTheSameChosenPalette_ReadsThePaletteFolderOnlyOnce()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);
        editor.Update();
        GameIs(CharacterSelect);
        editor.Update();
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        _fileSystemMock.Received(1).EnumerateFilesRecursively(AegisFolder);
    }

    [Fact]
    public void Update_NextMatchFindsTheColorsThisEditorLeft_KeepsTheGameColorsAsTheOriginal()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _memoryMock.BytesAt(PaletteMemory.FileAddress(TableArea(PlayerOneFirst), 2, CharacterFile), [1, 2, 3, 255]);
        PaletteColors gameColors = new PaletteTable(_memoryMock, Game.PaletteTableAddress(PlayerOneFirst)).Read(2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        GameIs(Match);
        editor.Update();
        GameIs(CharacterSelect);
        editor.Update();
        _settings.ChoosePalette("Aegis", 3, null);
        _memoryMock.Int32At(Game.PaletteIndexAddress(PlayerOneFirst), 2);
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.True(editor.Character(PlayerOneFirst)!.Original.SameAs(gameColors));
        Assert.Equal(gameColors.File(CharacterFile).ToArray(), FileBytes(PlayerOneFirst, 2));
    }

    [Fact]
    public void Character_InAMatch_KnowsWhoAndWhichColor()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerTwoFirst, Aegis, color: 4);
        GameIs(Match);
        editor.Update();

        // Act
        EditedCharacter? character = editor.Character(PlayerTwoFirst);

        // Assert
        Assert.NotNull(character);
        Assert.Equal((2, "Aegis", 4), (character.Player, character.Character.Name, character.Color));
    }

    [Fact]
    public void Character_ModdedCharacter_UsesTheFolderNamedAfterItsCode()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        ModdedCharacterInRoster(Kohaku, "mko");
        CharacterInSlot(PlayerOneFirst, Kohaku, color: 2);
        _fileSystemMock.DirectoryExists(KohakuFolder).Returns(true);
        _fileSystemMock.EnumerateFilesRecursively(KohakuFolder).Returns([KohakuFolder + @"\Red.impl"]);
        _fileSystemMock.ReadAllBytes(KohakuFolder + @"\Red.impl").Returns(ImplFile.Create(Kohaku, fill: 0x42));
        _settings.ChoosePalette("mko", 3, "Red");
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.Equal("Red", editor.Character(PlayerOneFirst)!.PaletteName);
        Assert.All(FileBytes(PlayerOneFirst, 2), value => Assert.Equal(0x42, value));
    }

    [Fact]
    public void Change_Color_RedrawsItOnTheNextFrame()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);

        // Act
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Red);
        editor.Update();

        // Assert
        Assert.Equal(Red, ShownColor(PlayerOneFirst, 2));
        Assert.Equal(Red, ShownColor(PlayerOneFirst, 1));
        Assert.Equal(1, PaletteIndex(PlayerOneFirst));
    }

    [Fact]
    public void Change_TwiceInOneFrame_SwitchesTheColorOnlyOnce()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);

        // Act
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Red);
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Blue);
        editor.Update();

        // Assert
        Assert.Equal(1, PaletteIndex(PlayerOneFirst));
        Assert.Equal(Blue, ShownColor(PlayerOneFirst, 1));
    }

    [Fact]
    public void Change_OnTwoFrames_SwitchesBackToTheChosenColor()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Red);
        editor.Update();

        // Act
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Blue);
        editor.Update();

        // Assert
        Assert.Equal(2, PaletteIndex(PlayerOneFirst));
        Assert.Equal(Blue, ShownColor(PlayerOneFirst, 2));
    }

    [Fact]
    public void Undo_AfterARememberedChange_RedrawsThePreviousColors()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        PaletteColor before = ShownColor(PlayerOneFirst, 2);
        editor.Remember(PlayerOneFirst);
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Red);
        editor.Update();

        // Act
        editor.Undo(PlayerOneFirst);
        editor.Update();

        // Assert
        Assert.Equal(before, ShownColor(PlayerOneFirst, 2));
        Assert.Equal(0, editor.Character(PlayerOneFirst)!.UndoCount);
    }

    [Fact]
    public void Reset_AfterAPalette_RedrawsTheGameColors()
    {
        // Arrange
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        PaletteColors gameColors = new PaletteTable(_memoryMock, Game.PaletteTableAddress(PlayerOneFirst)).Read(2);
        _settings.ChoosePalette("Aegis", 3, "Minato");
        PaletteEditor editor = CreatePaletteEditor();
        GameIs(Match);
        editor.Update();

        // Act
        editor.Reset(PlayerOneFirst);
        editor.Update();

        // Assert
        Assert.Equal(gameColors.File(CharacterFile).ToArray(), FileBytes(PlayerOneFirst, 2));
        Assert.Null(editor.Character(PlayerOneFirst)!.PaletteName);
    }

    [Fact]
    public void Wear_Palette_RedrawsItAndRemembersItsName()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Aegis, fill: 0x42));

        // Act
        editor.Wear(PlayerOneFirst, palette);
        editor.Update();

        // Assert
        Assert.All(FileBytes(PlayerOneFirst, 2), value => Assert.Equal(0x42, value));
        Assert.Equal("Minato", editor.Character(PlayerOneFirst)!.PaletteName);
    }

    [Fact]
    public void Highlight_Entry_FlashesThatEntry()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Blue);
        editor.Update();

        // Act
        editor.Highlight(PlayerOneFirst, CharacterFile, [Entry]);
        editor.Update();

        // Assert
        Assert.Equal(Magenta, ShownColor(PlayerOneFirst, PaletteIndex(PlayerOneFirst)));
    }

    [Fact]
    public void Highlight_SeveralEntries_FlashesEachOfThem()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        editor.Change(PlayerOneFirst, CharacterFile, Entry + 1, Blue);
        editor.Update();

        // Act
        editor.Highlight(PlayerOneFirst, CharacterFile, [Entry, Entry + 1]);
        editor.Update();

        // Assert
        Assert.Equal(Magenta, ShownColor(PlayerOneFirst, PaletteIndex(PlayerOneFirst), Entry + 1));
    }

    [Fact]
    public void Highlight_Entry_DimsTheOtherEntries()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        editor.Change(PlayerOneFirst, CharacterFile, Entry + 1, Red);
        editor.Update();

        // Act
        editor.Highlight(PlayerOneFirst, CharacterFile, [Entry]);
        editor.Update();

        // Assert
        Assert.Equal(new PaletteColor(38, 38, 38, 255), ShownColor(PlayerOneFirst, PaletteIndex(PlayerOneFirst), Entry + 1));
    }

    [Fact]
    public void Highlight_AfterOneBlink_FlashesTheOtherColor()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Blue);
        editor.Update();
        editor.Highlight(PlayerOneFirst, CharacterFile, [Entry]);

        // Act
        for (int frame = 0; frame < PaletteEditor.BlinkFrames; frame++)
            editor.Update();

        // Assert
        Assert.Equal(Cyan, ShownColor(PlayerOneFirst, PaletteIndex(PlayerOneFirst)));
    }

    [Fact]
    public void Change_DuringAHighlight_ShowsTheNewColorRightAway()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        editor.Highlight(PlayerOneFirst, CharacterFile, [Entry]);
        editor.Update();

        // Act
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Red);
        editor.Update();

        // Assert
        Assert.Equal(Red, ShownColor(PlayerOneFirst, PaletteIndex(PlayerOneFirst)));
    }

    [Fact]
    public void Highlight_AfterItsFrames_DrawsTheColorsAgain()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        editor.Change(PlayerOneFirst, CharacterFile, Entry + 1, Red);
        editor.Highlight(PlayerOneFirst, CharacterFile, [Entry]);

        // Act
        for (int frame = 0; frame <= PaletteEditor.HighlightFrames; frame++)
            editor.Update();

        // Assert
        Assert.Equal(Red, ShownColor(PlayerOneFirst, PaletteIndex(PlayerOneFirst), Entry + 1));
    }

    [Fact]
    public void Save_EditedColors_WritesThePaletteAndNamesTheCharacterColors()
    {
        // Arrange
        PaletteEditor editor = StartedEditor(color: 2);
        editor.Change(PlayerOneFirst, CharacterFile, Entry, Red);

        // Act
        string path = editor.Save(PlayerOneFirst, "Crimson", "Painter", "Red all over");

        // Assert
        Assert.Equal(AegisFolder + @"\Crimson.impl", path);
        _fileSystemMock.Received(1).WriteAllBytes(path, Arg.Is<byte[]>(bytes => ImplPalette.Parse("Crimson", bytes).Colors().Get(CharacterFile, Entry) == Red));
        Assert.Equal("Crimson", editor.Character(PlayerOneFirst)!.PaletteName);
    }

    [Fact]
    public void Update_ColorSetToRandom_WearsThePickedPalette()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, RandomPalette.Any);
        _pickBelowMock(2).Returns(0);
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.Equal("Minato", editor.Character(PlayerOneFirst)!.PaletteName);
        Assert.All(FileBytes(PlayerOneFirst, 2), value => Assert.Equal(0x42, value));
    }

    [Fact]
    public void Update_RandomColorLandingOnTheGameColors_KeepsTheGameColors()
    {
        // Arrange
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color: 2);
        _settings.ChoosePalette("Aegis", 3, RandomPalette.Any);
        _pickBelowMock(2).Returns(1);
        GameIs(Match);

        // Act
        editor.Update();

        // Assert
        Assert.Equal((null, false), (editor.Character(PlayerOneFirst)!.PaletteName, editor.Character(PlayerOneFirst)!.IsEdited));
    }

    private PaletteEditor StartedEditor(int color)
    {
        PaletteEditor editor = CreatePaletteEditor();
        CharacterInSlot(PlayerOneFirst, Aegis, color);
        GameIs(Match);
        editor.Update();
        return editor;
    }

    private void GameIs(int state) => _memoryMock.Int32At(GameObject + 0x118, state);

    private void CharacterInSlot(int slot, int characterIndex, int color, bool loaded = true)
    {
        uint character = FirstCharacter + (uint)slot * CharacterStride;
        _memoryMock.UInt32At(CharacterList + (uint)slot * 4, character);
        _memoryMock.Map(character, 0x100).Int32At(character + 0x34, characterIndex);
        _memoryMock.Int32At(Game.PaletteIndexAddress(slot), color);

        if (loaded)
            PaletteMemory.Lay(_memoryMock, Game.PaletteTableAddress(slot), TableArea(slot), color - 1, color, color + 1);
    }

    private void ModdedCharacterInRoster(int index, string code)
    {
        _memoryMock.Map(RosterGetter, 8).BytesAt(RosterGetter, [0xB8, 0, 0, 0, 0, 0xC3]).UInt32At(RosterGetter + 1, Roster);
        uint record = Roster + (uint)index * RosterRecordLength;
        _memoryMock.Map(record, (int)RosterRecordLength).BytesAt(record, Encoding.Latin1.GetBytes(code));
    }

    private int PaletteIndex(int slot) => _memoryMock.Int32From(Game.PaletteIndexAddress(slot));

    private byte[] FileBytes(int slot, int color) =>
        _memoryMock.BytesFrom(PaletteMemory.FileAddress(TableArea(slot), color, CharacterFile), PaletteMemory.FileLength);

    private PaletteColor ShownColor(int slot, int color, int entry = Entry) =>
        new PaletteTable(_memoryMock, Game.PaletteTableAddress(slot)).Read(color).Get(CharacterFile, entry);

    private static uint TableArea(int slot) => FirstTableArea + (uint)slot * TableAreaStride;
}
