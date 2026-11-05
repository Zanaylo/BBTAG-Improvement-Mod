using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BBTAG_IM.Core.Buttons;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Buttons;

internal static unsafe class ExtraButtonRows
{
    private const int ItemsSlot = 0x7C;
    private const int RowCountSlot = 0x80;
    private const int RowNameSlot = 0x84;
    private const int ItemLength = 0x10FC;
    private const int MenusNotBuilt = 0;
    private const int TagBufferLength = 0x20;
    private const int PressedButtonsOffset = 0x30;
    private const byte FirstLetterOfTheRowTags = (byte)'A';
    private const byte FirstLetterOfTheIconTags = (byte)'C';

    private static readonly Dictionary<nint, nint> s_items = [];
    private static readonly Dictionary<ExtraButton, nint> s_rowKeys = [];

    private static nint s_keyboardVtable;
    private static delegate* unmanaged[Thiscall]<nint, nint> s_constructItem;
    private static delegate* unmanaged[Thiscall]<nint, int, nint> s_controllerRowName;
    private static delegate* unmanaged[Thiscall]<nint, int, nint> s_keyboardRowName;
    private static delegate* unmanaged[Cdecl]<byte*, int, int> s_gameIcon;
    private static delegate* unmanaged[Thiscall]<nint, int, uint> s_gameBattleButtons;
    private static delegate* unmanaged[Cdecl]<nint> s_configRoot;
    private static byte* s_standInTag;

    public static void Install(CodeImage code, ILog log)
    {
        var sites = ButtonConfigCode.Locate(code);
        BindAfterTheGame(ButtonConfigDevice.Controller, sites.Controller.ApplyTail);
        BindAfterTheGame(ButtonConfigDevice.Keyboard, sites.Keyboard.ApplyTail);
        SendEToTheBattle(sites.BattleButtons);
        log.Info($"E follows the button settings saved for it (controller 0x{sites.Controller.ApplyTail:X8}, keyboard 0x{sites.Keyboard.ApplyTail:X8}) and reaches the battle through 0x{sites.BattleButtons:X8}.");

        if (*(int*)sites.MenusGuard != MenusNotBuilt)
        {
            log.Error("The button settings menus were already built when the mod started, so they show no E row.");
            return;
        }

        AddRows(sites);
        log.Info($"The button settings menus show an E row (controller menu 0x{sites.Controller.Vtable:X8}, keyboard menu 0x{sites.Keyboard.Vtable:X8}).");
    }

    private static void BindAfterTheGame(ButtonConfigDevice device, uint site)
    {
        var cave = CodeCave.Reserve(ButtonConfigCode.BlockLength, $"E button ({device})");
        cave.Put(0, ButtonConfigCode.ApplyTail(device, site, cave.Address));
        CodeCave.Redirect(site, ButtonConfigCode.Jump(site, cave.Address));
    }

    private static void SendEToTheBattle(uint entry) =>
        s_gameBattleButtons = (delegate* unmanaged[Thiscall]<nint, int, uint>)Detour(
            entry,
            ButtonConfigCode.BattleButtonsPrologue,
            (uint)(delegate* unmanaged[Thiscall]<nint, int, uint>)&BattleButtons,
            "battle buttons");

    private static uint Detour(uint entry, ReadOnlySpan<byte> prologue, uint replacement, string purpose)
    {
        var trampoline = CodeCave.Reserve(ButtonConfigCode.TrampolineLength, purpose);
        trampoline.Put(0, ButtonConfigCode.Trampoline(prologue, entry, trampoline.Address));
        CodeCave.Redirect(entry, ButtonConfigCode.EntryJump(prologue, entry, replacement));
        return trampoline.Address;
    }

    private static void AddRows(ButtonConfigSites sites)
    {
        foreach (var button in ExtraButtons.All)
            s_rowKeys[button] = Marshal.StringToHGlobalAnsi(button.RowKey);

        s_constructItem = (delegate* unmanaged[Thiscall]<nint, nint>)sites.ItemConstructor;
        s_keyboardVtable = (nint)sites.Keyboard.Vtable;

        foreach (var device in Enum.GetValues<ButtonConfigDevice>())
        {
            var menu = sites.For(device);
            Replace(menu.Vtable, ItemsSlot, (nint)(delegate* unmanaged[Thiscall]<nint, nint>)&Items);
            Replace(menu.Vtable, RowCountSlot, (nint)(delegate* unmanaged[Thiscall]<nint, int>)&RowCount);
            var gameRowName = (delegate* unmanaged[Thiscall]<nint, int, nint>)Replace(menu.Vtable, RowNameSlot, (nint)(delegate* unmanaged[Thiscall]<nint, int, nint>)&RowName);

            if (device == ButtonConfigDevice.Controller)
                s_controllerRowName = gameRowName;
            else
                s_keyboardRowName = gameRowName;
        }

        ShowTheBoundButtons(sites);
    }

    private static void ShowTheBoundButtons(ButtonConfigSites sites)
    {
        ProtectedMemory.Write((nint)sites.KeyboardDrawBound, [ButtonConfigCode.ShownKeyboardBound(*(byte*)sites.KeyboardDrawBound)]);

        var draw = CodeCave.Reserve(ButtonConfigCode.DrawBlockLength, "E row of the controller");
        draw.Put(0, ButtonConfigCode.ControllerDraw(sites.ControllerDraw, draw.Address));
        CodeCave.Redirect(sites.ControllerDraw.Site, ButtonConfigCode.ControllerDrawJump(sites.ControllerDraw.Site, draw.Address));

        s_configRoot = (delegate* unmanaged[Cdecl]<nint>)sites.ConfigRoot;
        s_standInTag = (byte*)NativeMemory.AllocZeroed(TagBufferLength);
        Encoding.ASCII.GetBytes(ExtraButtons.StandInIconTag).CopyTo(new Span<byte>(s_standInTag, TagBufferLength));
        s_gameIcon = (delegate* unmanaged[Cdecl]<byte*, int, int>)Detour(
            sites.IconResolver,
            ButtonConfigCode.IconResolverPrologue,
            (uint)(delegate* unmanaged[Cdecl]<byte*, int, int>)&Icon,
            "button icon lookup");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]
    private static uint BattleButtons(nint device, int argument) =>
        ExtraButtons.WithE(*(uint*)(device + PressedButtonsOffset), s_gameBattleButtons(device, argument));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Icon(byte* tag, int player)
    {
        if (tag[0] != FirstLetterOfTheRowTags && tag[0] != FirstLetterOfTheIconTags)
            return s_gameIcon(tag, player);

        var name = Marshal.PtrToStringAnsi((nint)tag) ?? string.Empty;

        if (name == ButtonIcons.ETag)
            return ButtonIcons.EIcon;

        if ((uint)player > 1 || ExtraButtons.FromIconTag(name) is not { } button)
            return s_gameIcon(tag, player);

        var table = (byte*)(s_configRoot() + ExtraButtons.ControllerTableOffset(player));
        var kept = table[ExtraButtons.StandInSlot];
        table[ExtraButtons.StandInSlot] = table[ExtraButtons.SlotOf(ButtonConfigDevice.Controller, button)];
        var icon = s_gameIcon(s_standInTag, player);
        table[ExtraButtons.StandInSlot] = kept;
        return icon;
    }

    private static nint Replace(uint vtable, int slot, nint replacement) => ProtectedMemory.SwapPointer((nint)(vtable + slot), replacement);

    private static ButtonConfigDevice DeviceOf(nint menu) =>
        *(nint*)menu == s_keyboardVtable ? ButtonConfigDevice.Keyboard : ButtonConfigDevice.Controller;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]
    private static int RowCount(nint menu) => ExtraButtons.Rows(DeviceOf(menu));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]
    private static nint Items(nint menu)
    {
        if (s_items.TryGetValue(menu, out var items))
            return items;

        var rows = ExtraButtons.Rows(DeviceOf(menu));
        items = (nint)NativeMemory.AllocZeroed((nuint)(rows * ItemLength));

        for (var row = 0; row < rows; row++)
            s_constructItem(items + row * ItemLength);

        s_items[menu] = items;
        return items;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]
    private static nint RowName(nint menu, int row)
    {
        var device = DeviceOf(menu);

        if (ExtraButtons.AtRow(device, row) is { } button)
            return s_rowKeys[button];

        return device == ButtonConfigDevice.Controller ? s_controllerRowName(menu, row) : s_keyboardRowName(menu, row);
    }
}
