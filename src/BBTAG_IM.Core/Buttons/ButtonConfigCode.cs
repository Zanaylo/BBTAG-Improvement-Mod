using System.Buffers.Binary;
using System.Text;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Buttons;

public static class ButtonConfigCode
{
    public const int ReplacedLength = 14;
    public const int BlockLength = 0x40;
    public const int SwitchReplacedLength = 9;
    public const int TagTableOffset = 0x40;
    public const int DrawBlockLength = 0x80;
    public const int TrampolineLength = 0x10;

    private const string Name = "Button settings menu";
    private const int JumpLength = 5;
    private const byte JumpNear = 0xE9;
    private const byte CallNear = 0xE8;
    private const byte NoOperation = 0x90;
    private const int VtableOffset = 7;
    private const int ItemConstructorCallOffset = 0x16;
    private const int GuardOffset = 2;
    private const int ConstructorCallOffset = 0x15;
    private const uint ConstructorReach = 0x1000;
    private const byte ControllerTableThroughEdi = 0x47;
    private const byte KeyboardTableThroughEbx = 0x43;
    private const int GameControllerSlots = 12;
    private const int KeyboardDrawBoundOffset = 2;
    private const int NotASlotJumpOffset = 5;
    private const int BufferOffset = 29;
    private const int StringConstructorCallOffset = 33;
    private const int CommonDrawJumpOffset = 45;
    private const int CommonDrawJumpEnd = 49;
    private const int ConfigRootCallOffset = 5;
    private const int TagLength = 0x10;

    private static readonly BytePattern ControllerMenuStore = BytePattern.Parse("E8 ?? ?? ?? ?? C7 06 ?? ?? ?? ?? 8D 7B 44 BE 0E 00 00 00 90 8B CF E8");
    private static readonly BytePattern KeyboardMenuStore = BytePattern.Parse("E8 ?? ?? ?? ?? C7 06 ?? ?? ?? ?? BF 12 00 00 00 8D 73 44 8D 49 00 8B CE E8");
    private static readonly BytePattern ControllerApplyTail = BytePattern.Parse("0F BE 47 0B 8B CE 8B 16 50 6A 1B FF 52 34 5F 5E 5D C2 04 00");
    private static readonly BytePattern KeyboardApplyTail = BytePattern.Parse("0F BE 43 0F 8B CE 8B 16 50 6A 1B FF 52 34 8B 06 8B CE 85 FF");
    private static readonly BytePattern MenusGetter = BytePattern.Parse("83 3D ?? ?? ?? ?? FF 75 1E B9 ?? ?? ?? ?? C7 45 FC 00 00 00 00 E8");
    private static readonly BytePattern KeyboardDraw = BytePattern.Parse("83 F9 0F 7F ?? E8 ?? ?? ?? ?? 8B 8D ?? ?? ?? ?? 03 85 ?? ?? ?? ?? 8D 0C 49 0F BE 84 C8 10 42 00 00");
    private static readonly BytePattern ControllerDrawSwitch = BytePattern.Parse("83 FE 0B 0F 8F ?? ?? ?? ?? 0F 87 ?? ?? ?? ?? FF 24 B5 ?? ?? ?? ?? 68 ?? ?? ?? ?? 8D 8D ?? ?? ?? ?? E8 ?? ?? ?? ?? 8D 85 ?? ?? ?? ?? E9");
    private static readonly BytePattern IconResolverEntry = BytePattern.Parse("55 8B EC 83 EC 34 53 56 8B 75 08 B9 08 00 00 00");
    private static readonly BytePattern ControllerApplyStart = BytePattern.Parse("55 8B EC 56 57 E8 ?? ?? ?? ?? 8B 75 08 8D 8E 4C 03 00 00 8D 0C 89 8D 3C 88");
    private static readonly BytePattern BattleButtonsEntry = BytePattern.Parse("8B 01 53 56 FF 50 4C 8B D8 E8 ?? ?? ?? ?? 8B D3 8B CB");

    private static readonly byte[] ControllerReplaced = [0x0F, 0xBE, 0x47, 0x0B, 0x8B, 0xCE, 0x8B, 0x16, 0x50, 0x6A, 0x1B, 0xFF, 0x52, 0x34];
    private static readonly byte[] KeyboardReplaced = [0x0F, 0xBE, 0x43, 0x0F, 0x8B, 0xCE, 0x8B, 0x16, 0x50, 0x6A, 0x1B, 0xFF, 0x52, 0x34];

    public static ReadOnlySpan<byte> IconResolverPrologue => [0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x34];

    public static ReadOnlySpan<byte> BattleButtonsPrologue => [0x8B, 0x01, 0x53, 0x56, 0xFF, 0x50, 0x4C];

    public static ButtonConfigSites Locate(CodeImage image)
    {
        var controllerStore = image.FindSingle(ControllerMenuStore, $"{Name} for controllers");
        var keyboardStore = image.FindSingle(KeyboardMenuStore, $"{Name} for keyboards");

        return new ButtonConfigSites(
            new MenuButtonSites(ReadUInt32(image, controllerStore + VtableOffset), image.FindSingle(ControllerApplyTail, "Controller button settings")),
            new MenuButtonSites(ReadUInt32(image, keyboardStore + VtableOffset), image.FindSingle(KeyboardApplyTail, "Keyboard button settings")),
            image.CallTarget(controllerStore + ItemConstructorCallOffset, $"{Name} item"),
            MenusGuard(image, controllerStore),
            image.FindSingle(KeyboardDraw, $"{Name} keyboard rows") + KeyboardDrawBoundOffset,
            ControllerRows(image),
            image.FindSingle(IconResolverEntry, "Button icon lookup"),
            image.CallTarget(image.FindSingle(ControllerApplyStart, "Controller button settings start") + ConfigRootCallOffset, "Saved settings"),
            image.FindSingle(BattleButtonsEntry, "Battle buttons"));
    }

    public static byte ShownKeyboardBound(byte gameBound) => (byte)(gameBound + ExtraButtons.All.Count);

    public static byte[] ApplyTail(ButtonConfigDevice device, uint site, uint cave)
    {
        var table = device == ButtonConfigDevice.Controller ? ControllerTableThroughEdi : KeyboardTableThroughEbx;
        byte[] body =
        [
            .. device == ButtonConfigDevice.Controller ? ControllerReplaced : KeyboardReplaced,
            .. ExtraButtons.All.SelectMany(button => Bind(table, ExtraButtons.SlotOf(device, button), button.Logical)),
        ];
        var end = cave + (uint)body.Length + JumpLength;

        return [.. body, JumpNear, .. Address(site + ReplacedLength - end)];
    }

    public static byte[] Jump(uint site, uint cave) => JumpPadded(site, cave, ReplacedLength);

    public static byte[] ControllerDraw(ControllerRowDrawing drawing, uint cave)
    {
        var lastSlot = (byte)(GameControllerSlots + ExtraButtons.All.Count - 1);
        var code = new List<byte> { 0x83, 0xFE, GameControllerSlots - 1, 0x0F, 0x8F, 0x05, 0x00, 0x00, 0x00 };
        AddRelative(code, JumpNear, cave, drawing.Site + SwitchReplacedLength);
        code.AddRange([0x83, 0xFE, lastSlot, 0x0F, 0x8F]);
        code.AddRange(Address(drawing.NotASlot - (cave + (uint)code.Count + sizeof(uint))));
        code.AddRange([0xFF, 0x34, 0xB5, .. Address(cave + TagTableOffset - GameControllerSlots * sizeof(uint))]);
        code.AddRange([0x8D, 0x8D, .. BitConverter.GetBytes(drawing.Buffer)]);
        AddRelative(code, CallNear, cave, drawing.StringConstructor);
        code.AddRange([0x8D, 0x85, .. BitConverter.GetBytes(drawing.Buffer)]);
        AddRelative(code, JumpNear, cave, drawing.CommonDraw);

        var block = new byte[DrawBlockLength];
        code.CopyTo(block);
        var tags = TagTableOffset + ExtraButtons.All.Count * sizeof(uint);

        for (var index = 0; index < ExtraButtons.All.Count; index++)
        {
            var tag = tags + index * TagLength;
            Address(cave + (uint)tag).CopyTo(block, TagTableOffset + index * sizeof(uint));
            Encoding.ASCII.GetBytes(ExtraButtons.All[index].IconTag).CopyTo(block, tag);
        }

        return block;
    }

    public static byte[] ControllerDrawJump(uint site, uint cave) => JumpPadded(site, cave, SwitchReplacedLength);

    public static byte[] Trampoline(ReadOnlySpan<byte> prologue, uint entry, uint cave) =>
        [.. prologue, JumpNear, .. Address(entry + (uint)prologue.Length - (cave + (uint)prologue.Length + JumpLength))];

    public static byte[] EntryJump(ReadOnlySpan<byte> prologue, uint entry, uint target) => JumpPadded(entry, target, prologue.Length);

    private static byte[] JumpPadded(uint site, uint target, int length) =>
        [JumpNear, .. Address(target - (site + JumpLength)), .. Enumerable.Repeat(NoOperation, length - JumpLength)];

    private static byte[] Bind(byte table, int slot, byte logical) =>
    [
        0x0F, 0xBE, table, (byte)slot,
        0x83, 0xF8, ExtraButtons.Unassigned,
        0x74, 0x0A,
        0x8B, 0xCE,
        0x8B, 0x16,
        0x50,
        0x6A, logical,
        0xFF, 0x52, 0x34,
    ];

    private static ControllerRowDrawing ControllerRows(CodeImage image)
    {
        var site = image.FindSingle(ControllerDrawSwitch, $"{Name} controller rows");

        return new ControllerRowDrawing(
            site,
            site + SwitchReplacedLength + ReadUInt32(image, site + NotASlotJumpOffset),
            (int)ReadUInt32(image, site + BufferOffset),
            image.CallTarget(site + StringConstructorCallOffset, $"{Name} row text"),
            site + CommonDrawJumpEnd + ReadUInt32(image, site + CommonDrawJumpOffset));
    }

    private static void AddRelative(List<byte> code, byte opcode, uint cave, uint target)
    {
        code.Add(opcode);
        code.AddRange(Address(target - (cave + (uint)code.Count + sizeof(uint))));
    }

    private static uint MenusGuard(CodeImage image, uint controllerStore)
    {
        var getters = image.FindAll(MenusGetter).Where(getter => Builds(image, getter, controllerStore)).ToArray();

        if (getters.Length != 1)
            throw new GameLayoutException($"{Name} creation was found {getters.Length} times in the game code. {GameLayoutException.UnsupportedVersion}");

        return ReadUInt32(image, getters[0] + GuardOffset);
    }

    private static bool Builds(CodeImage image, uint getter, uint controllerStore)
    {
        var constructor = image.CallTarget(getter + ConstructorCallOffset, $"{Name} creation");
        return constructor <= controllerStore && controllerStore - constructor < ConstructorReach;
    }

    private static uint ReadUInt32(CodeImage image, uint address) => BinaryPrimitives.ReadUInt32LittleEndian(image.Slice(address, sizeof(uint)));

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
