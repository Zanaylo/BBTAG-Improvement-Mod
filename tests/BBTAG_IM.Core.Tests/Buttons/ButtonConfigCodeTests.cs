using System.Text;
using BBTAG_IM.Core.Buttons;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Buttons;

public class ButtonConfigCodeTests
{
    private const uint Cave = 0x30000000;
    private const uint Target = 0x31000000;
    private const uint ControllerTail = 0x006B3FA5;
    private const uint KeyboardTail = 0x006B40D6;
    private const uint MenusAt = 0x006FB000;
    private const uint GetterAt = 0x006FC000;
    private const uint OtherGetterAt = 0x006FD000;
    private const uint ControllerMenuVtable = 0x00B6A22C;
    private const uint KeyboardMenuVtable = 0x00B6A2D4;
    private const uint MenusGuard = 0x0120D7A0;
    private const uint KeyboardDrawAt = 0x008D1C60;
    private const uint MenuKeyboardDrawAt = 0x008D16A8;
    private const uint SwitchAt = 0x008D1913;
    private const uint NotASlot = 0x008D1A6D;
    private const uint StringConstructor = 0x00413540;
    private const uint CommonDraw = 0x008D145C;
    private const int RowBuffer = -0x678;
    private const uint ResolverAt = 0x008BB020;
    private const uint PadApplyAt = 0x006B3EC0;
    private const uint ConfigRoot = 0x006B2B50;
    private const uint BattleButtonsAt = 0x00690210;

    private static readonly byte[] KeyboardDrawBytes = [0x83, 0xF9, 0x0F, 0x7F, 0x53, 0xE8, 0xD6, 0x0E, 0xDE, 0xFF, 0x8B, 0x8D, 0x4C, 0xD6, 0xFF, 0xFF, 0x03, 0x85, 0x78, 0xD6, 0xFF, 0xFF, 0x8D, 0x0C, 0x49, 0x0F, 0xBE, 0x84, 0xC8, 0x10, 0x42, 0x00, 0x00];
    private static readonly byte[] MenuKeyboardDrawBytes = [0x83, 0xF9, 0x0F, 0x7F, 0x7A, 0xE8, 0x8E, 0x14, 0xDE, 0xFF, 0x8B, 0x8D, 0x4C, 0xD6, 0xFF, 0xFF, 0x03, 0x85, 0x78, 0xD6, 0xFF, 0xFF, 0x8D, 0x0C, 0x89, 0x0F, 0xBE, 0x84, 0x88, 0x68, 0x42, 0x00, 0x00];
    private static readonly byte[] ControllerTailBytes = [0x0F, 0xBE, 0x47, 0x0B, 0x8B, 0xCE, 0x8B, 0x16, 0x50, 0x6A, 0x1B, 0xFF, 0x52, 0x34, 0x5F, 0x5E, 0x5D, 0xC2, 0x04, 0x00];
    private static readonly byte[] KeyboardTailBytes = [0x0F, 0xBE, 0x43, 0x0F, 0x8B, 0xCE, 0x8B, 0x16, 0x50, 0x6A, 0x1B, 0xFF, 0x52, 0x34, 0x8B, 0x06, 0x8B, 0xCE, 0x85, 0xFF];
    private static readonly byte[] ResolverBytes = [0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x34, 0x53, 0x56, 0x8B, 0x75, 0x08, 0xB9, 0x08, 0x00, 0x00, 0x00];
    private static readonly byte[] BattleButtonsBytes = [0x8B, 0x01, 0x53, 0x56, 0xFF, 0x50, 0x4C, 0x8B, 0xD8, 0xE8, 0, 0, 0, 0, 0x8B, 0xD3, 0x8B, 0xCB];

    [Fact]
    public void Locate_GameCode_FindsBothMenus()
    {
        // Arrange
        CodeImage image = GameCode();

        // Act
        ButtonConfigSites sites = ButtonConfigCode.Locate(image);

        // Assert
        Assert.Equal((ControllerMenuVtable, KeyboardMenuVtable), (sites.Controller.Vtable, sites.Keyboard.Vtable));
    }

    [Fact]
    public void Locate_GameCode_FindsTheApplyTails()
    {
        // Arrange
        CodeImage image = GameCode();

        // Act
        ButtonConfigSites sites = ButtonConfigCode.Locate(image);

        // Assert
        Assert.Equal((ControllerTail, KeyboardTail), (sites.Controller.ApplyTail, sites.Keyboard.ApplyTail));
    }

    [Fact]
    public void Locate_GameCode_FindsTheItemConstructorAndTheGuardOfTheMenus()
    {
        // Arrange
        CodeImage image = GameCode();

        // Act
        ButtonConfigSites sites = ButtonConfigCode.Locate(image);

        // Assert
        Assert.Equal((MenusAt + 0x40, MenusGuard), (sites.ItemConstructor, sites.MenusGuard));
    }

    [Fact]
    public void Locate_GameCode_FindsWhereTheRowsDrawTheirButton()
    {
        // Arrange
        CodeImage image = GameCode();

        // Act
        ButtonConfigSites sites = ButtonConfigCode.Locate(image);

        // Assert
        Assert.Equal(KeyboardDrawAt + 2, sites.KeyboardDrawBound);
        Assert.Equal(new ControllerRowDrawing(SwitchAt, NotASlot, RowBuffer, StringConstructor, CommonDraw), sites.ControllerDraw);
    }

    [Fact]
    public void Locate_GameCode_FindsTheIconResolverTheConfigRootAndTheBattleButtons()
    {
        // Arrange
        CodeImage image = GameCode();

        // Act
        ButtonConfigSites sites = ButtonConfigCode.Locate(image);

        // Assert
        Assert.Equal((ResolverAt, ConfigRoot, BattleButtonsAt), (sites.IconResolver, sites.ConfigRoot, sites.BattleButtons));
    }

    [Fact]
    public void Locate_NoMenuCode_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(MenusAt, new byte[0x40])]);

        // Act
        Exception? exception = Record.Exception(() => ButtonConfigCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void ShownKeyboardBound_GameBound_AlsoShowsTheESlot()
    {
        // Act
        byte bound = ButtonConfigCode.ShownKeyboardBound(0x0F);

        // Assert
        Assert.Equal((byte)0x10, bound);
    }

    [Fact]
    public void ApplyTail_Controller_RunsTheReplacedBindFirst()
    {
        // Act
        byte[] code = ButtonConfigCode.ApplyTail(ButtonConfigDevice.Controller, ControllerTail, Cave);

        // Assert
        Assert.Equal(ControllerTailBytes[..ButtonConfigCode.ReplacedLength], code[..ButtonConfigCode.ReplacedLength]);
    }

    [Fact]
    public void ApplyTail_Controller_BindsESkippingAnUnassignedSlot()
    {
        // Arrange
        byte[] expected = [0x0F, 0xBE, 0x47, 0x0C, 0x83, 0xF8, 0xFF, 0x74, 0x0A, 0x8B, 0xCE, 0x8B, 0x16, 0x50, 0x6A, 0x10, 0xFF, 0x52, 0x34];

        // Act
        byte[] code = ButtonConfigCode.ApplyTail(ButtonConfigDevice.Controller, ControllerTail, Cave);

        // Assert
        Assert.Equal(expected, code[ButtonConfigCode.ReplacedLength..(ButtonConfigCode.ReplacedLength + expected.Length)]);
    }

    [Fact]
    public void ApplyTail_Keyboard_ReadsEFromItsSlotThroughEbx()
    {
        // Arrange
        byte[] expected = [0x0F, 0xBE, 0x43, 0x10, 0x83, 0xF8, 0xFF, 0x74, 0x0A, 0x8B, 0xCE, 0x8B, 0x16, 0x50, 0x6A, 0x10, 0xFF, 0x52, 0x34];

        // Act
        byte[] code = ButtonConfigCode.ApplyTail(ButtonConfigDevice.Keyboard, KeyboardTail, Cave);

        // Assert
        Assert.Equal(expected, code[ButtonConfigCode.ReplacedLength..(ButtonConfigCode.ReplacedLength + expected.Length)]);
    }

    [Fact]
    public void ApplyTail_Controller_JumpsBackAfterTheReplacedBytes()
    {
        // Act
        byte[] code = ButtonConfigCode.ApplyTail(ButtonConfigDevice.Controller, ControllerTail, Cave);

        // Assert
        uint end = Cave + (uint)code.Length;
        Assert.Equal((byte)0xE9, code[^5]);
        Assert.Equal(ControllerTail + ButtonConfigCode.ReplacedLength, end + BitConverter.ToUInt32(code, code.Length - 4));
    }

    [Fact]
    public void Jump_Site_JumpsToTheCaveAndFillsTheRestWithNops()
    {
        // Act
        byte[] jump = ButtonConfigCode.Jump(ControllerTail, Cave);

        // Assert
        Assert.Equal(ButtonConfigCode.ReplacedLength, jump.Length);
        Assert.Equal(Cave, ControllerTail + 5 + BitConverter.ToUInt32(jump, 1));
        Assert.All(jump[5..], value => Assert.Equal((byte)0x90, value));
    }

    [Fact]
    public void ControllerDraw_GameSlot_KeepsTheGameSwitch()
    {
        // Arrange
        byte[] expected = [0x83, 0xFE, 0x0B, 0x0F, 0x8F, 0x05, 0, 0, 0, 0xE9, .. Relative(Cave + 9, SwitchAt + ButtonConfigCode.SwitchReplacedLength)];

        // Act
        byte[] code = ButtonConfigCode.ControllerDraw(Drawing(), Cave);

        // Assert
        Assert.Equal(expected, code[..expected.Length]);
    }

    [Fact]
    public void ControllerDraw_ESlot_DrawsItsIconTagLikeTheGameRows()
    {
        // Arrange
        byte[] expected =
        [
            0x83, 0xFE, 0x0C, 0x0F, 0x8F, .. Relative(Cave + 0x11, NotASlot, 6),
            0xFF, 0x34, 0xB5, .. BitConverter.GetBytes(Cave + ButtonConfigCode.TagTableOffset - 12 * 4),
            0x8D, 0x8D, .. BitConverter.GetBytes(RowBuffer),
            0xE8, .. Relative(Cave + 0x24, StringConstructor),
            0x8D, 0x85, .. BitConverter.GetBytes(RowBuffer),
            0xE9, .. Relative(Cave + 0x2F, CommonDraw),
        ];

        // Act
        byte[] code = ButtonConfigCode.ControllerDraw(Drawing(), Cave);

        // Assert
        Assert.Equal(expected, code[0x0E..(0x0E + expected.Length)]);
    }

    [Fact]
    public void ControllerDraw_TagTable_PointsAtTheETag()
    {
        // Act
        byte[] code = ButtonConfigCode.ControllerDraw(Drawing(), Cave);

        // Assert
        Assert.Equal("AttackE", TextAt(code, BitConverter.ToUInt32(code, ButtonConfigCode.TagTableOffset) - Cave));
    }

    [Fact]
    public void Trampoline_IconResolver_RunsTheMovedPrologueThenJumpsBack()
    {
        // Arrange
        byte[] expected = [0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x34, 0xE9, .. Relative(Cave + 6, ResolverAt + 6)];

        // Act
        byte[] code = ButtonConfigCode.Trampoline(ButtonConfigCode.IconResolverPrologue, ResolverAt, Cave);

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void Trampoline_BattleButtons_RunsTheMovedPrologueThenJumpsBack()
    {
        // Arrange
        byte[] expected = [0x8B, 0x01, 0x53, 0x56, 0xFF, 0x50, 0x4C, 0xE9, .. Relative(Cave + 7, BattleButtonsAt + 7)];

        // Act
        byte[] code = ButtonConfigCode.Trampoline(ButtonConfigCode.BattleButtonsPrologue, BattleButtonsAt, Cave);

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void EntryJump_Entry_JumpsToTheModAndPadsTheMovedPrologue()
    {
        // Arrange
        byte[] expected = [0xE9, .. Relative(BattleButtonsAt, Target), 0x90, 0x90];

        // Act
        byte[] jump = ButtonConfigCode.EntryJump(ButtonConfigCode.BattleButtonsPrologue, BattleButtonsAt, Target);

        // Assert
        Assert.Equal(expected, jump);
    }

    private static CodeImage GameCode()
    {
        byte[] menus = new byte[0x100];
        Put(menus, 0x00, [0xE8, 0, 0, 0, 0, 0xC7, 0x06, .. BitConverter.GetBytes(ControllerMenuVtable), 0x8D, 0x7B, 0x44, 0xBE, 0x0E, 0, 0, 0, 0x90, 0x8B, 0xCF, 0xE8, .. Relative(MenusAt + 0x16, MenusAt + 0x40)]);
        Put(menus, 0x60, [0xE8, 0, 0, 0, 0, 0xC7, 0x06, .. BitConverter.GetBytes(KeyboardMenuVtable), 0xBF, 0x12, 0, 0, 0, 0x8D, 0x73, 0x44, 0x8D, 0x49, 0x00, 0x8B, 0xCE, 0xE8]);

        return new CodeImage(
        [
            new CodeBlock(ControllerTail, ControllerTailBytes),
            new CodeBlock(KeyboardTail, KeyboardTailBytes),
            new CodeBlock(MenusAt, menus),
            new CodeBlock(GetterAt, Getter(MenusGuard, MenusAt - 0x10, GetterAt)),
            new CodeBlock(OtherGetterAt, Getter(0x01300000, 0x00900000, OtherGetterAt)),
            new CodeBlock(MenuKeyboardDrawAt, MenuKeyboardDrawBytes),
            new CodeBlock(KeyboardDrawAt, KeyboardDrawBytes),
            new CodeBlock(SwitchAt, Switch()),
            new CodeBlock(ResolverAt, ResolverBytes),
            new CodeBlock(PadApplyAt, [0x55, 0x8B, 0xEC, 0x56, 0x57, 0xE8, .. Relative(PadApplyAt + 5, ConfigRoot), 0x8B, 0x75, 0x08, 0x8D, 0x8E, 0x4C, 0x03, 0x00, 0x00, 0x8D, 0x0C, 0x89, 0x8D, 0x3C, 0x88]),
            new CodeBlock(BattleButtonsAt, BattleButtonsBytes),
        ]);
    }

    private static ControllerRowDrawing Drawing() => new(SwitchAt, NotASlot, RowBuffer, StringConstructor, CommonDraw);

    private static byte[] Switch() =>
    [
        0x83, 0xFE, 0x0B,
        0x0F, 0x8F, .. Relative(SwitchAt + 3, NotASlot, 6),
        0x0F, 0x87, 0x10, 0x05, 0x00, 0x00,
        0xFF, 0x24, 0xB5, 0xC8, 0x1E, 0x8D, 0x00,
        0x68, 0xB8, 0xD0, 0xB8, 0x00,
        0x8D, 0x8D, .. BitConverter.GetBytes(RowBuffer),
        0xE8, .. Relative(SwitchAt + 33, StringConstructor),
        0x8D, 0x85, .. BitConverter.GetBytes(RowBuffer),
        0xE9, .. Relative(SwitchAt + 44, CommonDraw),
    ];

    private static byte[] Getter(uint guard, uint constructor, uint at) =>
        [0x83, 0x3D, .. BitConverter.GetBytes(guard), 0xFF, 0x75, 0x1E, 0xB9, 0, 0, 0, 0, 0xC7, 0x45, 0xFC, 0, 0, 0, 0, 0xE8, .. Relative(at + 0x15, constructor)];

    private static string TextAt(byte[] block, uint offset) =>
        Encoding.ASCII.GetString(block, (int)offset, Array.IndexOf(block, (byte)0, (int)offset) - (int)offset);

    private static byte[] Relative(uint call, uint target, uint length = 5) => BitConverter.GetBytes(target - (call + length));

    private static void Put(byte[] block, int offset, byte[] bytes) => bytes.CopyTo(block, offset);
}
