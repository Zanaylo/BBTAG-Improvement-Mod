using BBTAG_IM.Core.Buttons;

namespace BBTAG_IM.Core.Tests.Buttons;

public class ExtraButtonsTests
{
    [Theory]
    [InlineData(ButtonConfigDevice.Controller, 14)]
    [InlineData(ButtonConfigDevice.Keyboard, 18)]
    public void AtRow_RowAfterTheGameRows_IsE(ButtonConfigDevice device, int row)
    {
        // Act
        ExtraButton? button = ExtraButtons.AtRow(device, row);

        // Assert
        Assert.Same(ExtraButtons.E, button);
    }

    [Theory]
    [InlineData(ButtonConfigDevice.Controller, 13)]
    [InlineData(ButtonConfigDevice.Controller, 15)]
    [InlineData(ButtonConfigDevice.Keyboard, 19)]
    [InlineData(ButtonConfigDevice.Keyboard, 0)]
    public void AtRow_GameRowOrPastTheEnd_IsNull(ButtonConfigDevice device, int row)
    {
        // Act
        ExtraButton? button = ExtraButtons.AtRow(device, row);

        // Assert
        Assert.Null(button);
    }

    [Theory]
    [InlineData(ButtonConfigDevice.Controller, 15)]
    [InlineData(ButtonConfigDevice.Keyboard, 19)]
    public void Rows_Device_AddsTheExtraButtonToTheGameRows(ButtonConfigDevice device, int expected)
    {
        // Act
        int rows = ExtraButtons.Rows(device);

        // Assert
        Assert.Equal(expected, rows);
    }

    [Theory]
    [InlineData(ButtonConfigDevice.Controller, 12)]
    [InlineData(ButtonConfigDevice.Keyboard, 16)]
    public void SlotOf_E_IsTheRowMinusTheExitAndDefaultRows(ButtonConfigDevice device, int expected)
    {
        // Act
        int slot = ExtraButtons.SlotOf(device, ExtraButtons.E);

        // Assert
        Assert.Equal(expected, slot);
    }

    [Fact]
    public void TextEntries_E_NamesItsRowKey()
    {
        // Act
        IReadOnlyList<KeyValuePair<string, string>> entries = ExtraButtons.TextEntries;

        // Assert
        Assert.Equal([new("KYCF_AttackE", "^mCMDS;Button E")], entries);
    }

    [Fact]
    public void FromIconTag_ETag_IsE()
    {
        // Act
        ExtraButton? button = ExtraButtons.FromIconTag("AttackE");

        // Assert
        Assert.Same(ExtraButtons.E, button);
    }

    [Theory]
    [InlineData("AttackAB")]
    [InlineData("AttackSP")]
    [InlineData("")]
    public void FromIconTag_OtherTag_IsNull(string tag)
    {
        // Act
        ExtraButton? button = ExtraButtons.FromIconTag(tag);

        // Assert
        Assert.Null(button);
    }

    [Fact]
    public void StandIn_GameTag_IsTheAPlusBShortcutSlot()
    {
        // Act
        (string Tag, int Slot) standIn = (ExtraButtons.StandInIconTag, ExtraButtons.StandInSlot);

        // Assert
        Assert.Equal(("AttackAB", 5), standIn);
    }

    [Theory]
    [InlineData(0, 0x41F0)]
    [InlineData(1, 0x4204)]
    public void ControllerTableOffset_Player_IsTheGameSavedTableOfThatPlayer(int player, int expected)
    {
        // Act
        int offset = ExtraButtons.ControllerTableOffset(player);

        // Assert
        Assert.Equal(expected, offset);
    }

    [Fact]
    public void E_GameInput_IsLogical0x10AndTheBattleE()
    {
        // Act
        (byte Logical, uint Pressed, ushort Battle) e = (ExtraButtons.E.Logical, ExtraButtons.E.PressedMask, ExtraButtons.E.BattleBit);

        // Assert
        Assert.Equal(((byte)0x10, 0x10000u, (ushort)0x100), e);
    }

    [Theory]
    [InlineData(0x0000u, (ushort)0x0015, (ushort)0x0015)]
    [InlineData(0x10000u, (ushort)0x0015, (ushort)0x0115)]
    [InlineData(0x11000u, (ushort)0x0215, (ushort)0x0315)]
    public void WithE_PressedButtons_AddsEToTheBattleButtonsWhenItIsHeld(uint pressed, ushort battle, ushort expected)
    {
        // Act
        uint buttons = ExtraButtons.WithE(pressed, battle);

        // Assert
        Assert.Equal(expected, buttons);
    }
}
