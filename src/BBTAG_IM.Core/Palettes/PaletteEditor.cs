using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Palettes;

public sealed class PaletteEditor(GameView game, CharacterRoster roster, Func<ImprovementSettings> settings, PaletteCatalog catalog, ILog log, Func<int, int> pickBelow)
{
    public const int HighlightFrames = 90;
    public const int BlinkFrames = 8;

    private const int HighestColor = 0xFF;

    private readonly EditedCharacter?[] _characters = new EditedCharacter?[GameView.CharacterSlots];
    private readonly PaletteTable[] _tables = Enumerable.Range(0, GameView.CharacterSlots)
        .Select(slot => new PaletteTable(game.Memory, game.PaletteTableAddress(slot)))
        .ToArray();
    private readonly Dictionary<(string Folder, int Color), LeftColors> _leftColors = [];

    private HighlightedEntries? _highlight;

    public bool InMatch { get; private set; }

    public EditedCharacter? Character(int slot) => _characters[slot];

    public void Update()
    {
        InMatch = game.State == GameState.Match;

        if (!InMatch)
        {
            Leave();
            return;
        }

        for (var slot = 0; slot < GameView.CharacterSlots; slot++)
            _characters[slot] = Current(slot);

        CountDownHighlight();

        foreach (var character in _characters)
        {
            if (character is { NeedsRedraw: true })
                Redraw(character);
        }
    }

    public void Remember(int slot) => _characters[slot]?.Remember();

    public void Change(int slot, int file, int entry, PaletteColor color)
    {
        if (_highlight?.Slot == slot)
            EndHighlight();

        _characters[slot]?.Change(file, entry, color);
    }

    public void Undo(int slot) => _characters[slot]?.Undo();

    public void Reset(int slot) => Wear(slot, null);

    public void Wear(int slot, ImplPalette? palette)
    {
        if (_characters[slot] is not { } character)
            return;

        character.Remember();
        character.Wear(palette is null ? character.Original.Copy() : character.Original.Wearing(palette), palette?.Name);
    }

    public void Highlight(int slot, int file, IReadOnlyCollection<int> entries)
    {
        if (_characters[slot] is not { } character)
            return;

        EndHighlight();
        _highlight = new HighlightedEntries(slot, file, entries.ToArray());
        character.NeedsRedraw = true;
    }

    public string Save(int slot, string name, string creator, string description)
    {
        if (_characters[slot] is not { } character)
            throw new InvalidOperationException($"Slot {slot} has no character to save.");

        var palette = ImplPalette.Create(name, character.Character.Index, creator, description, character.Colors);
        var path = catalog.Library.Save(character.Character.Folder, palette);
        character.Name(name);
        return path;
    }

    private EditedCharacter? Current(int slot)
    {
        var current = _characters[slot];

        if (current is not null && Table(slot).FileList == current.FileList)
            return current;

        return Start(slot);
    }

    private EditedCharacter? Start(int slot)
    {
        if (game.Character(slot) is null || game.CharacterIndex(slot) is not { } index)
            return null;

        var color = game.Memory.ReadInt32(game.PaletteIndexAddress(slot));
        var partner = PaletteTable.PartnerOf(color);
        var table = Table(slot);

        if (color is < 0 or > HighestColor || !table.IsLoaded(color) || !table.IsLoaded(partner) || table.FileList is not { } fileList)
            return null;

        var identity = roster.Identify(index);
        var (original, leftByUs) = GameColors(identity, color, table.Read(color));
        var (partnerOriginal, partnerLeftByUs) = GameColors(identity, partner, table.Read(partner));
        var files = Enumerable.Range(0, PaletteColors.FileCount).Select(file => table.HasFile(color, file)).ToArray();
        var character = new EditedCharacter(slot, identity, color, fileList, original, partnerOriginal, files)
        {
            NeedsRedraw = leftByUs || partnerLeftByUs,
        };

        WearChosenPalette(character);
        return character;
    }

    private (PaletteColors Colors, bool LeftByUs) GameColors(CharacterIdentity identity, int color, PaletteColors found)
    {
        if (_leftColors.TryGetValue((identity.Folder, color), out var left) && found.SameAs(left.Shown))
            return (left.Original, true);

        return (found, false);
    }

    private void WearChosenPalette(EditedCharacter character)
    {
        var folder = character.Character.Folder;

        if (settings().PaletteFor(folder, character.Color + 1) is not { } paletteName)
            return;

        var palettes = catalog.Load(folder);

        if (RandomPalette.IsChoice(paletteName))
        {
            WearRandomPalette(character, paletteName, palettes);
            return;
        }

        if (palettes.Find(paletteName) is not { } palette)
        {
            log.Info($"{folder} color {character.Color + 1} should use the palette {paletteName}, but it is not in the palettes folder.");
            return;
        }

        Wear(character, palette);
    }

    private void WearRandomPalette(EditedCharacter character, string choice, CharacterPalettes palettes)
    {
        if (RandomPalette.Pick(choice, palettes.Palettes, pickBelow) is not { } palette)
        {
            log.Info($"Player {character.Player} {character.Character.Name} color {character.Color + 1}: the random pick kept the game colors.");
            return;
        }

        Wear(character, palette);
    }

    private void Wear(EditedCharacter character, ImplPalette palette)
    {
        character.Wear(character.Original.Wearing(palette), palette.Name);
        log.Info($"Player {character.Player} {character.Character.Name} color {character.Color + 1}: palette {palette.Name}.");
    }

    private void Redraw(EditedCharacter character)
    {
        var table = Table(character.Slot);
        var colors = _highlight is { } highlight && highlight.Slot == character.Slot
            ? character.Colors.Flashing(highlight.File, highlight.Entries, highlight.IsLit)
            : character.Colors;

        table.Write(character.Color, colors);
        table.Write(character.Partner, colors);

        var indexAddress = game.PaletteIndexAddress(character.Slot);
        var shown = game.Memory.ReadInt32(indexAddress) == character.Color ? character.Partner : character.Color;
        game.Memory.WriteInt32(indexAddress, shown);

        character.NeedsRedraw = false;
        character.LastShown = colors;

        if (character.Redrawn)
            return;

        character.Redrawn = true;
        log.Info($"Player {character.Player} {character.Character.Name} color {character.Color + 1} is redrawn through color {character.Partner + 1}.");
    }

    private void CountDownHighlight()
    {
        if (_highlight is not { } highlight)
            return;

        highlight.FramesShown++;

        if (highlight.FramesShown >= HighlightFrames)
        {
            EndHighlight();
            return;
        }

        if (highlight.Blinks && _characters[highlight.Slot] is { } character)
            character.NeedsRedraw = true;
    }

    private void EndHighlight()
    {
        if (_highlight is not { } highlight)
            return;

        _highlight = null;

        if (_characters[highlight.Slot] is { } character)
            character.NeedsRedraw = true;
    }

    private void Leave()
    {
        _highlight = null;

        for (var slot = 0; slot < GameView.CharacterSlots; slot++)
        {
            if (_characters[slot] is { LastShown: { } shown } character)
                PutBack(character, shown);

            _characters[slot] = null;
        }
    }

    private void PutBack(EditedCharacter character, PaletteColors shown)
    {
        _leftColors[(character.Character.Folder, character.Color)] = new LeftColors(shown, character.Original);
        _leftColors[(character.Character.Folder, character.Partner)] = new LeftColors(shown, character.PartnerOriginal);

        var indexAddress = game.PaletteIndexAddress(character.Slot);

        if (game.Memory.ReadInt32(indexAddress) == character.Partner)
            game.Memory.WriteInt32(indexAddress, character.Color);
    }

    private PaletteTable Table(int slot) => _tables[slot];

    private sealed record LeftColors(PaletteColors Shown, PaletteColors Original);

    private sealed class HighlightedEntries(int slot, int file, int[] entries)
    {
        public int Slot => slot;

        public int File => file;

        public IReadOnlyCollection<int> Entries => entries;

        public int FramesShown { get; set; }

        public bool IsLit => FramesShown / BlinkFrames % 2 == 0;

        public bool Blinks => FramesShown % BlinkFrames == 0;
    }
}
