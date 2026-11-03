using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class HitboxEditsTests
{
    private const uint GameObject = 0x00E59100;
    private const uint BattleObject = 0x014A77A0;
    private const uint CharacterList = 0x0157D3F8;
    private const uint CameraList = 0x00C0A788;
    private const uint EntityList = 0x07000000;
    private const uint FirstEntity = 0x09000000;
    private const uint EntityStride = 0x2000;
    private const int EntityLength = 0x1440;
    private const uint ArchiveAddress = 0x4D2F6908;
    private const uint FirstBuffer = 0x01000000;
    private const uint BufferStride = 0x10000;
    private const string Code = "brg";
    private const string Sprite = "rg000_04";
    private const string OtherSprite = "rg000_05";
    private const string SavePath = @"D:\BBTAG\mods\DEV\data\char\char_brg_col\rg000_04.jonbin";

    private static readonly JonbinBox Body = new(0, -46, -386, 61, 390);
    private static readonly JonbinBox Legs = new(0, -68, -244, 140, 263);
    private static readonly JonbinBox Fist = new(1, 20, -300, 90, 60);

    private readonly FakeGameMemory _memory = new();
    private readonly IScriptBuffers _scriptBuffersMock = Substitute.For<IScriptBuffers>();
    private readonly IBoxReaders _boxReadersMock = Substitute.For<IBoxReaders>();
    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly byte[] _spriteFile = new JonbinBytes().Image("rg000_04.bmp").Sections(1, 0, 2).Box(0, -46, -386, 61, 390).Box(0, -68, -244, 140, 263).Build();
    private readonly byte[] _otherFile = new JonbinBytes().Image("rg000_05.bmp").Box(0, 1, 2, 3, 4).Build();
    private int _buffers;

    public HitboxEditsTests()
    {
        _memory.Map(BattleObject, 0xA5000).UInt32At(BattleObject + 0xA4210, EntityList);
        _memory.Map(EntityList, GameView.EntityCount * 4);
        byte[] archive = SpriteFiles.Fpac(($"{Sprite}.jonbin", _spriteFile), ($"{OtherSprite}.jonbin", _otherFile));
        _memory.Map(ArchiveAddress, archive.Length).BytesAt(ArchiveAddress, archive);
        _scriptBuffersMock.Allocate(Arg.Any<byte[]>()).Returns(call => MapBuffer(call.Arg<byte[]>()));
    }

    [Fact]
    public void Read_SpriteNotEdited_ReturnsTheBoxesTheGameLoaded()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();

        // Act
        Jonbin jonbin = edits.Read(sprite);

        // Assert
        Assert.Equal([Body, Legs], jonbin.Boxes);
    }

    [Fact]
    public void Read_SpriteTheArchiveHasNot_ThrowsInvalidCollisionDataException()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, "rg999_00");
        HitboxEdits edits = CreateHitboxEdits();

        // Act
        Exception? exception = Record.Exception(() => edits.Read(sprite));

        // Assert
        InvalidCollisionDataException invalid = Assert.IsType<InvalidCollisionDataException>(exception);
        Assert.Equal("The game has no collision file named rg999_00.jonbin.", invalid.Message);
    }

    [Fact]
    public void Find_SpriteTheArchiveHasNot_ReturnsNull()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, "rg999_00");
        HitboxEdits edits = CreateHitboxEdits();

        // Act
        Jonbin? jonbin = edits.Find(sprite);

        // Assert
        Assert.Null(jonbin);
    }

    [Fact]
    public void Find_SpriteOfTheArchive_ReturnsItsBoxes()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, OtherSprite);
        HitboxEdits edits = CreateHitboxEdits();

        // Act
        Jonbin? jonbin = edits.Find(sprite);

        // Assert
        Assert.Equal([new JonbinBox(0, 1, 2, 3, 4)], jonbin?.Boxes);
    }

    [Fact]
    public void Change_NewBoxes_MakesTheGameReadThemFromItsOwnBuffer()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();

        // Act
        edits.Change(sprite, Code, [Body, Legs, Fist]);

        // Assert
        byte[] expected = Jonbin.Read(_spriteFile).With([Body, Legs, Fist]).Write();
        ArchiveEntry entry = new LoadedArchive(_memory, ArchiveAddress).Find($"{Sprite}.jonbin")!;
        Assert.Equal(FirstBuffer, entry.Address);
        Assert.Equal(expected, _memory.BytesFrom(FirstBuffer, expected.Length));
        _boxReadersMock.Received(1).Load(sprite.Entity, FirstBuffer);
    }

    [Fact]
    public void Change_SameSpriteTwice_KeepsWritingIntoTheSameBuffer()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();
        edits.Change(sprite, Code, [Body, Legs, Fist]);

        // Act
        edits.Change(sprite, Code, [Body]);

        // Assert
        _scriptBuffersMock.Received(1).Allocate(Arg.Any<byte[]>());
        Assert.Equal([Body], edits.Read(sprite).Boxes);
        Assert.Equal(Jonbin.Read(_spriteFile).With([Body]).Write().Length, new LoadedArchive(_memory, ArchiveAddress).Find($"{Sprite}.jonbin")!.Size);
    }

    [Fact]
    public void Change_AnotherObjectShowsTheSameSprite_ReloadsItToo()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        EntitySprite mirror = EntityInSlot(1, Sprite);
        EntitySprite other = EntityInSlot(2, OtherSprite);
        HitboxEdits edits = CreateHitboxEdits();

        // Act
        edits.Change(sprite, Code, [Body]);

        // Assert
        _boxReadersMock.Received(1).Load(mirror.Entity, FirstBuffer);
        _boxReadersMock.DidNotReceive().Load(other.Entity, Arg.Any<uint>());
    }

    [Fact]
    public void Change_MoreBoxesThanTheBufferHolds_ThrowsInvalidCollisionDataException()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();
        JonbinBox[] boxes = [.. Enumerable.Repeat(Fist, HitboxEdits.ExtraBoxes + 3)];

        // Act
        Exception? exception = Record.Exception(() => edits.Change(sprite, Code, boxes));

        // Assert
        InvalidCollisionDataException invalid = Assert.IsType<InvalidCollisionDataException>(exception);
        Assert.Equal($"A sprite takes up to {HitboxEdits.ExtraBoxes} boxes more than it came with.", invalid.Message);
        Assert.Equal(_spriteFile.Length, new LoadedArchive(_memory, ArchiveAddress).Find($"{Sprite}.jonbin")!.Size);
    }

    [Fact]
    public void Undo_ChangedSprite_MakesTheGameReadItsOwnFileAgain()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();
        uint original = sprite.JonbinAddress;
        edits.Change(sprite, Code, [Body, Legs, Fist]);

        // Act
        edits.Undo(sprite);

        // Assert
        ArchiveEntry entry = new LoadedArchive(_memory, ArchiveAddress).Find($"{Sprite}.jonbin")!;
        Assert.Equal((original, _spriteFile.Length), (entry.Address, entry.Size));
        Assert.Equal([Body, Legs], edits.Read(sprite).Boxes);
        Assert.False(edits.IsChanged(sprite));
        _boxReadersMock.Received(1).Load(sprite.Entity, original);
    }

    [Fact]
    public void IsChanged_ChangedSprite_IsTrue()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();

        // Act
        edits.Change(sprite, Code, [Body]);

        // Assert
        Assert.True(edits.IsChanged(sprite));
    }

    [Fact]
    public void IsChanged_BoxesPutBackByHand_IsFalse()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();
        edits.Change(sprite, Code, [Body, Legs, Fist]);

        // Act
        edits.Change(sprite, Code, [Body, Legs]);

        // Assert
        Assert.False(edits.IsChanged(sprite));
    }

    [Fact]
    public void SaveAll_ChangedSprite_WritesItIntoTheDevFolder()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();
        edits.Change(sprite, Code, [Body, Fist]);

        // Act
        IReadOnlyList<string> saved = edits.SaveAll();

        // Assert
        byte[] expected = Jonbin.Read(_spriteFile).With([Body, Fist]).Write();
        Assert.Equal([SavePath], saved);
        _fileSystemMock.Received(1).WriteAllBytes(SavePath, Arg.Is<byte[]>(bytes => bytes.SequenceEqual(expected)));
        Assert.Empty(edits.Unsaved);
    }

    [Fact]
    public void SaveAll_NothingChangedSinceTheLastSave_WritesNothing()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();
        edits.Change(sprite, Code, [Body, Fist]);
        edits.SaveAll();
        _fileSystemMock.ClearReceivedCalls();

        // Act
        IReadOnlyList<string> saved = edits.SaveAll();

        // Assert
        Assert.Empty(saved);
        _fileSystemMock.DidNotReceive().WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void Unsaved_ChangedSprite_ListsItsName()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();

        // Act
        edits.Change(sprite, Code, [Body]);

        // Assert
        Assert.Equal([Sprite], edits.Unsaved);
    }

    [Fact]
    public void Reset_AfterTheMatch_ReleasesTheBuffersAndForgetsTheChanges()
    {
        // Arrange
        EntitySprite sprite = EntityInSlot(0, Sprite);
        HitboxEdits edits = CreateHitboxEdits();
        edits.Change(sprite, Code, [Body]);

        // Act
        edits.Reset();

        // Assert
        _scriptBuffersMock.Received(1).Release(FirstBuffer);
        Assert.Empty(edits.Unsaved);
    }

    private HitboxEdits CreateHitboxEdits() =>
        new(new GameView(_memory, new GameAddresses(GameObject, BattleObject, CharacterList, CameraList)), DevFolder.ForMod(@"D:\BBTAG\mods\BBTAG-Improvement-Mod"), _scriptBuffersMock, _boxReadersMock, _fileSystemMock);

    private EntitySprite EntityInSlot(int slot, string sprite)
    {
        uint entity = FirstEntity + (uint)slot * EntityStride;
        uint jonbin = new LoadedArchive(_memory, ArchiveAddress).Find($"{sprite}.jonbin")?.Address ?? 0;
        _memory.Map(entity, EntityLength)
            .UInt32At(entity + 0x40, jonbin)
            .UInt32At(entity + 0x154, ArchiveAddress)
            .BytesAt(entity + 0x1420, Encoding.ASCII.GetBytes(sprite + "\0"))
            .UInt32At(EntityList + (uint)slot * 4, entity);
        return new EntitySprite(entity, ArchiveAddress, sprite, jonbin);
    }

    private uint MapBuffer(byte[] bytes)
    {
        uint address = FirstBuffer + (uint)_buffers++ * BufferStride;
        _memory.Map(address, bytes.Length).BytesAt(address, bytes);
        return address;
    }
}
