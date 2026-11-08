using System.Text;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class MoveRegistrationsTests
{
    private const int Register = 14001;
    private const int EndRegister = 14002;
    private const int Priority = 15024;
    private const int Counterattack = 12059;

    [Fact]
    public void Of_MatchInitWithTwoRegistrations_ReturnsTheirNamesAndOnlyTheirCommands()
    {
        // Arrange
        ScriptBinary script = Read(new ScriptBytes()
            .State("A").Command(1)
            .Subroutine("MatchInit")
            .Command(ScriptTestDatabase.Damage, 5)
            .Command(Register, "NmlAtk5A", 0x16).Command(ScriptTestDatabase.Damage, 7).Command(EndRegister)
            .Command(Register, "NmlAtk2A", 0x13).Command(EndRegister)
            .Command(9)
            .Build());
        byte[] expected = new ScriptBytes()
            .Command(Register, "NmlAtk5A", 0x16).Command(ScriptTestDatabase.Damage, 7).Command(EndRegister)
            .Command(Register, "NmlAtk2A", 0x13).Command(EndRegister)
            .Build()[sizeof(int)..];

        // Act
        MoveRegistrations registrations = MoveRegistrations.Of(script);

        // Assert
        Assert.Equal(["NmlAtk5A", "NmlAtk2A"], registrations.Names);
        Assert.Equal(expected, registrations.Commands);
    }

    [Fact]
    public void Of_CommandsThatNameMovesAfterTheRegistrations_KeepsThemInOrder()
    {
        // Arrange
        ScriptBinary script = Read(new ScriptBytes()
            .State("A").Command(1)
            .Subroutine("MatchInit")
            .Command(Register, "NmlAtk5A", 0x16).Command(EndRegister)
            .Command(ScriptTestDatabase.Damage, 3)
            .Command(Priority, "NmlAtk5A", "NmlAtk5AA", 10000000)
            .Command(Counterattack, 0, "NmlAtk5A")
            .Command(9)
            .Build());
        byte[] expected = new ScriptBytes()
            .Command(Register, "NmlAtk5A", 0x16).Command(EndRegister)
            .Command(Priority, "NmlAtk5A", "NmlAtk5AA", 10000000)
            .Command(Counterattack, 0, "NmlAtk5A")
            .Build()[sizeof(int)..];

        // Act
        MoveRegistrations registrations = MoveRegistrations.Of(script);

        // Assert
        Assert.Equal(expected, registrations.Commands);
    }

    [Fact]
    public void Of_RegistrationInAnotherSubroutine_LeavesItOut()
    {
        // Arrange
        ScriptBinary script = Read(new ScriptBytes()
            .State("A").Command(1)
            .Subroutine("MatchInit").Command(Register, "NmlAtk5A", 0x16).Command(EndRegister).Command(9)
            .Subroutine("Other").Command(Register, "NmlAtk2A", 0x13).Command(EndRegister).Command(9)
            .Build());

        // Act
        MoveRegistrations registrations = MoveRegistrations.Of(script);

        // Assert
        Assert.Equal(["NmlAtk5A"], registrations.Names);
    }

    [Fact]
    public void Of_NoMatchInit_ReturnsNoRegistrations()
    {
        // Arrange
        ScriptBinary script = Read(new ScriptBytes().State("A").Command(1).Build());

        // Act
        MoveRegistrations registrations = MoveRegistrations.Of(script);

        // Assert
        Assert.Empty(registrations.Names);
        Assert.Empty(registrations.Commands);
    }

    [Fact]
    public void SameAs_DifferentCommandInsideARegistration_ReturnsFalse()
    {
        // Arrange
        MoveRegistrations first = MoveRegistrations.Of(Read(MatchInitWith(7)));
        MoveRegistrations second = MoveRegistrations.Of(Read(MatchInitWith(8)));

        // Act
        bool same = first.SameAs(second);

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void SameAs_SameRegistrations_ReturnsTrue()
    {
        // Arrange
        MoveRegistrations first = MoveRegistrations.Of(Read(MatchInitWith(7)));
        MoveRegistrations second = MoveRegistrations.Of(Read(MatchInitWith(7)));

        // Act
        bool same = first.SameAs(second);

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void AsSubroutine_Name_WrapsTheCommandsInAStartAndAnEnd()
    {
        // Arrange
        MoveRegistrations registrations = MoveRegistrations.Of(Read(MatchInitWith(7)));
        byte[] expected = new ScriptBytes().Subroutine("IMMoves").Build()[sizeof(int)..]
            .Concat(registrations.Commands)
            .Concat(new ScriptBytes().Command(9).Build()[sizeof(int)..])
            .ToArray();

        // Act
        byte[] subroutine = registrations.AsSubroutine("IMMoves");

        // Assert
        Assert.Equal(expected, subroutine);
        Assert.Equal("IMMoves", Encoding.Latin1.GetString(subroutine, sizeof(int), 7));
    }

    private static byte[] MatchInitWith(int damage) => new ScriptBytes()
        .State("A").Command(1)
        .Subroutine("MatchInit").Command(Register, "NmlAtk5A", 0x16).Command(ScriptTestDatabase.Damage, damage).Command(EndRegister).Command(9)
        .Build();

    private static ScriptBinary Read(byte[] bytes) => ScriptBinary.Read(bytes, ScriptTestDatabase.Create());
}
