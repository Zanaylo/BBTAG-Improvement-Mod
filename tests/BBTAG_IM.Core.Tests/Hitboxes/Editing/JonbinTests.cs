using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class JonbinTests
{
    [Fact]
    public void Read_GameFile_ReadsTheBoxesOfEveryType()
    {
        // Arrange
        byte[] bytes = new JonbinBytes().Image("rg000_00.bmp").Sections(1, 0, 2)
            .Box(0, -46, -386, 61, 390).Box(0, -68, -244, 140, 263).Box(1, 20, -300, 90, 60).Box(5, 1, 2, 3, 4).Build();

        // Act
        Jonbin jonbin = Jonbin.Read(bytes);

        // Assert
        Assert.Equal(JonbinBytes.TypeCount, jonbin.TypeCount);
        Assert.Equal(
            [new JonbinBox(0, -46, -386, 61, 390), new JonbinBox(0, -68, -244, 140, 263), new JonbinBox(1, 20, -300, 90, 60), new JonbinBox(5, 1, 2, 3, 4)],
            jonbin.Boxes);
    }

    [Fact]
    public void Read_SpritePieces_ReadsWhereEachPieceOfThePictureIsDrawn()
    {
        // Arrange
        byte[] bytes = new JonbinBytes().Image("rg000_00.bmp").Image("vr_rg_ef000.bmp").Sections(1, 0, 0)
            .Chunk(0, 23, 511, 489, -256, -449, 511, 489, 0).Chunk(480, 0, 32, 12, -40, -360, 32, 12, 1, image: 1).Box(0, -46, -386, 61, 390).Build();

        // Act
        Jonbin jonbin = Jonbin.Read(bytes);

        // Assert
        Assert.Equal(
            [new SpriteChunk(0, 23, 511, 489, -256, -449, 511, 489, 0, 0), new SpriteChunk(480, 0, 32, 12, -40, -360, 32, 12, 1, 1)],
            jonbin.Chunks);
        Assert.Equal(["rg000_00.bmp", "vr_rg_ef000.bmp"], jonbin.Images);
    }

    [Fact]
    public void Pieces_ChunksOnSeveralPictures_PairsEachChunkWithItsOwnPicture()
    {
        // Arrange
        Jonbin jonbin = Jonbin.Read(new JonbinBytes().Image("vr_car_ef000.bmp").Image("Car500_00.bmp")
            .Chunk(0, 0, 864, 640, 432, -589, -864, 640, 0, image: 1).Chunk(0, 0, 64, 64, 10, -100, 64, 64, 0, image: 0).Build());

        // Act
        IReadOnlyList<SpritePiece> pieces = jonbin.Pieces;

        // Assert
        Assert.Equal(
            [
                new SpritePiece("Car500_00.bmp", new SpriteChunk(0, 0, 864, 640, 432, -589, -864, 640, 0, 1)),
                new SpritePiece("vr_car_ef000.bmp", new SpriteChunk(0, 0, 64, 64, 10, -100, 64, 64, 0, 0)),
            ],
            pieces);
    }

    [Fact]
    public void Pieces_ChunksOnSeveralLayers_ComeFromTheBackLayerToTheFront()
    {
        // Arrange
        Jonbin jonbin = Jonbin.Read(new JonbinBytes().Image("rg000_00.bmp")
            .Chunk(0, 0, 10, 10, 0, 0, 10, 10, 2).Chunk(10, 0, 10, 10, 0, 0, 10, 10, 0).Build());

        // Act
        IReadOnlyList<SpritePiece> pieces = jonbin.Pieces;

        // Assert
        Assert.Equal([0, 2], pieces.Select(piece => piece.Chunk.Layer));
    }

    [Fact]
    public void Pieces_ChunkOnThePlaceholderPicture_LeavesItOut()
    {
        // Arrange
        Jonbin jonbin = Jonbin.Read(new JonbinBytes().Image("Car000_00.bmp").Image("Car999_99.bmp")
            .Chunk(0, 0, 864, 640, 430, -592, -864, 640, 0, image: 0).Chunk(623, 52, 17, 23, 14, -329, -17, 23, 0, image: 1).Build());

        // Act
        IReadOnlyList<SpritePiece> pieces = jonbin.Pieces;

        // Assert
        Assert.Equal("Car000_00.bmp", Assert.Single(pieces).Image);
    }

    [Fact]
    public void Pieces_ChunkOnAPictureTheFileDoesNotName_LeavesItOut()
    {
        // Arrange
        Jonbin jonbin = Jonbin.Read(new JonbinBytes().Image("rg000_00.bmp").Chunk(0, 0, 10, 10, 0, 0, 10, 10, 0, image: 1).Build());

        // Act
        IReadOnlyList<SpritePiece> pieces = jonbin.Pieces;

        // Assert
        Assert.Empty(pieces);
    }

    [Fact]
    public void Write_NothingChanged_GivesBackTheSameBytes()
    {
        // Arrange
        byte[] bytes = new JonbinBytes().Image("rg000_00.bmp").Image("rg999_99.bmp").Sections(2, 1, 3)
            .Box(0, -46, -386, 61, 390).Box(1, 20, -300, 90, 60).Tail(0xAA, 0xBB).Build();
        Jonbin jonbin = Jonbin.Read(bytes);

        // Act
        byte[] written = jonbin.Write();

        // Assert
        Assert.Equal(bytes, written);
    }

    [Fact]
    public void With_AnAddedHitbox_WritesItsCountAndItsBox()
    {
        // Arrange
        Jonbin jonbin = Jonbin.Read(new JonbinBytes().Image("rg000_00.bmp").Sections(1, 0, 2).Box(0, -46, -386, 61, 390).Build());

        // Act
        Jonbin changed = jonbin.With([.. jonbin.Boxes, new JonbinBox(1, 20, -300, 90, 60)]);

        // Assert
        byte[] expected = new JonbinBytes().Image("rg000_00.bmp").Sections(1, 0, 2).Box(0, -46, -386, 61, 390).Box(1, 20, -300, 90, 60).Build();
        Assert.Equal(expected, changed.Write());
    }

    [Fact]
    public void With_BoxesOutOfOrder_KeepsThemGroupedByType()
    {
        // Arrange
        Jonbin jonbin = Jonbin.Read(new JonbinBytes().Image("rg000_00.bmp").Build());

        // Act
        Jonbin changed = jonbin.With([new JonbinBox(1, 20, -300, 90, 60), new JonbinBox(0, -46, -386, 61, 390), new JonbinBox(1, 5, 6, 7, 8)]);

        // Assert
        Assert.Equal([new JonbinBox(0, -46, -386, 61, 390), new JonbinBox(1, 20, -300, 90, 60), new JonbinBox(1, 5, 6, 7, 8)], changed.Boxes);
    }

    [Fact]
    public void With_ABoxOfATypeTheFileHasNot_ThrowsInvalidCollisionDataException()
    {
        // Arrange
        Jonbin jonbin = Jonbin.Read(new JonbinBytes().Image("rg000_00.bmp").Build());

        // Act
        Exception? exception = Record.Exception(() => jonbin.With([new JonbinBox(JonbinBytes.TypeCount, 0, 0, 1, 1)]));

        // Assert
        InvalidCollisionDataException invalid = Assert.IsType<InvalidCollisionDataException>(exception);
        Assert.Equal("Collision files have no box type 17.", invalid.Message);
    }

    [Fact]
    public void Read_AnotherKindOfFile_ThrowsInvalidCollisionDataException()
    {
        // Arrange
        byte[] bytes = "HIP\0........"u8.ToArray();

        // Act
        Exception? exception = Record.Exception(() => Jonbin.Read(bytes));

        // Assert
        InvalidCollisionDataException invalid = Assert.IsType<InvalidCollisionDataException>(exception);
        Assert.Equal("It is not a collision file.", invalid.Message);
    }

    [Fact]
    public void Read_FileCutShort_ThrowsInvalidCollisionDataException()
    {
        // Arrange
        byte[] bytes = new JonbinBytes().Image("rg000_00.bmp").Sections(1, 0, 2).Box(0, -46, -386, 61, 390).Build();

        // Act
        Exception? exception = Record.Exception(() => Jonbin.Read(bytes.AsSpan(0, bytes.Length - 1)));

        // Assert
        InvalidCollisionDataException invalid = Assert.IsType<InvalidCollisionDataException>(exception);
        Assert.Equal("The collision file ends too early.", invalid.Message);
    }
}
