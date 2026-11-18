using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Tests.IO;

public class MemoryFileTests
{
    private static readonly byte[] Bytes = [1, 2, 3, 4, 5, 6];

    public MemoryFile CreateMemoryFile() => new(Bytes);

    [Fact]
    public void Read_FromTheStart_CopiesTheBytesAndMovesThePosition()
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();
        byte[] buffer = new byte[4];

        // Act
        int read = file.Read(buffer);

        // Assert
        Assert.Equal((4, "1,2,3,4", 4L), (read, string.Join(",", buffer), file.Position));
    }

    [Fact]
    public void Read_BufferLongerThanWhatIsLeft_CopiesWhatIsLeft()
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();
        file.Seek(4, SeekOrigin.Begin);
        byte[] buffer = new byte[10];

        // Act
        int read = file.Read(buffer);

        // Assert
        Assert.Equal((2, (byte)5, (byte)6), (read, buffer[0], buffer[1]));
    }

    [Fact]
    public void Read_AtTheEnd_CopiesNothing()
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();
        file.Seek(0, SeekOrigin.End);

        // Act
        int read = file.Read(new byte[4]);

        // Assert
        Assert.Equal(0, read);
    }

    [Fact]
    public void Read_PositionPastTheEnd_CopiesNothing()
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();
        file.Seek(100, SeekOrigin.Begin);

        // Act
        int read = file.Read(new byte[4]);

        // Assert
        Assert.Equal(0, read);
    }

    [Fact]
    public void ReadAt_Offset_CopiesFromThereAndMovesThePositionAfterIt()
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();
        byte[] buffer = new byte[2];

        // Act
        int read = file.ReadAt(3, buffer);

        // Assert
        Assert.Equal((2, "4,5", 5L), (read, string.Join(",", buffer), file.Position));
    }

    [Fact]
    public void ReadAt_OffsetPastTheEnd_CopiesNothing()
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();

        // Act
        int read = file.ReadAt(6, new byte[2]);

        // Assert
        Assert.Equal(0, read);
    }

    [Theory]
    [InlineData(2, SeekOrigin.Begin, 2)]
    [InlineData(-1, SeekOrigin.End, 5)]
    [InlineData(10, SeekOrigin.End, 16)]
    public void Seek_FromAnOrigin_ReturnsTheNewPosition(long distance, SeekOrigin origin, long expected)
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();

        // Act
        long? position = file.Seek(distance, origin);

        // Assert
        Assert.Equal(expected, position);
    }

    [Fact]
    public void Seek_FromTheCurrentPosition_MovesFromThere()
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();
        file.Seek(3, SeekOrigin.Begin);

        // Act
        long? position = file.Seek(-2, SeekOrigin.Current);

        // Assert
        Assert.Equal(1, position);
    }

    [Fact]
    public void Seek_BeforeTheStart_ReturnsNullAndKeepsThePosition()
    {
        // Arrange
        MemoryFile file = CreateMemoryFile();
        file.Seek(2, SeekOrigin.Begin);

        // Act
        long? position = file.Seek(-3, SeekOrigin.Current);

        // Assert
        Assert.Equal((null, 2L), (position, file.Position));
    }

    [Fact]
    public void Length_File_IsTheByteCount()
    {
        // Act
        long length = CreateMemoryFile().Length;

        // Assert
        Assert.Equal(6, length);
    }
}
