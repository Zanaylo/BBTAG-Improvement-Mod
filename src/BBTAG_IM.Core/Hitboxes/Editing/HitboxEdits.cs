using System.Runtime.InteropServices;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.Live;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed class HitboxEdits(GameView game, DevFolder dev, IScriptBuffers buffers, IBoxReaders readers, IFileSystem files)
{
    public const int ExtraBoxes = 64;

    private const int EntityListLength = GameView.EntityCount * sizeof(uint);

    private readonly Dictionary<SpriteKey, SpriteEdit> _sprites = [];

    public IReadOnlyList<string> Unsaved => [.. _sprites.Values.Where(edit => edit.IsUnsaved).Select(edit => edit.Name).Order(StringComparer.OrdinalIgnoreCase)];

    public Jonbin Read(EntitySprite sprite) => Edit(sprite).Current;

    public Jonbin? Find(EntitySprite sprite) => Known(sprite)?.Current;

    public bool IsChanged(EntitySprite sprite) => _sprites.TryGetValue(SpriteKey.Of(sprite), out var edit) && edit.IsChanged;

    public void Change(EntitySprite sprite, string characterCode, IEnumerable<JonbinBox> boxes)
    {
        var edit = Edit(sprite);
        var changed = edit.Current.With(boxes);
        var bytes = changed.Write();

        if (bytes.Length > edit.Capacity)
            throw new InvalidCollisionDataException($"A sprite takes up to {ExtraBoxes} boxes more than it came with.");

        edit.Buffer ??= buffers.Allocate(new byte[edit.Capacity]);
        edit.SavePath ??= dev.CollisionFile(characterCode, sprite.FileName);
        game.Memory.Write(edit.Buffer.Value, bytes);
        edit.Show(changed, bytes);
        Show(sprite, edit, edit.Buffer.Value, bytes.Length);
    }

    public void Undo(EntitySprite sprite)
    {
        if (!_sprites.TryGetValue(SpriteKey.Of(sprite), out var edit) || !edit.IsChanged)
            return;

        edit.Show(edit.Original, edit.OriginalBytes);
        Show(sprite, edit, edit.Entry.Address, edit.Entry.Size);
    }

    public IReadOnlyList<string> SaveAll()
    {
        List<string> saved = [];

        foreach (var edit in _sprites.Values.Where(edit => edit.IsUnsaved && edit.SavePath is not null))
        {
            files.WriteAllBytes(edit.SavePath!, edit.CurrentBytes);
            edit.Saved = edit.CurrentBytes;
            saved.Add(edit.SavePath!);
        }

        return saved;
    }

    public void Reset()
    {
        foreach (var edit in _sprites.Values)
        {
            if (edit.Buffer is { } buffer)
                buffers.Release(buffer);
        }

        _sprites.Clear();
    }

    private SpriteEdit Edit(EntitySprite sprite) =>
        Known(sprite) ?? throw new InvalidCollisionDataException($"The game has no collision file named {sprite.FileName}.");

    private SpriteEdit? Known(EntitySprite sprite)
    {
        var key = SpriteKey.Of(sprite);

        if (_sprites.TryGetValue(key, out var known))
            return known;

        var archive = new LoadedArchive(game.Memory, sprite.Archive);

        if (archive.Find(sprite.FileName) is not { } entry)
            return null;

        var bytes = game.Memory.ReadBytes(entry.Address, entry.Size);
        var edit = new SpriteEdit(sprite.Name, archive, entry, Jonbin.Read(bytes), bytes);
        _sprites.Add(key, edit);
        return edit;
    }

    private void Show(EntitySprite sprite, SpriteEdit edit, uint jonbin, int size)
    {
        edit.Archive.Point(edit.Entry, jonbin, size);

        foreach (var entity in EntitiesShowing(sprite))
            readers.Load(entity, jonbin);
    }

    private List<uint> EntitiesShowing(EntitySprite sprite)
    {
        List<uint> showing = [];

        if (game.EntityList is not { } list || !game.Memory.CanRead(list, EntityListLength))
            return showing;

        var bytes = game.Memory.ReadBytes(list, EntityListLength);

        foreach (var entity in MemoryMarshal.Cast<byte, uint>(bytes))
        {
            if (entity != 0 && EntitySprite.Read(game.Memory, entity) is { } shown && SpriteKey.Of(shown) == SpriteKey.Of(sprite))
                showing.Add(entity);
        }

        return showing;
    }

    private readonly record struct SpriteKey(uint Archive, string Name)
    {
        public static SpriteKey Of(EntitySprite sprite) => new(sprite.Archive, sprite.Name);
    }

    private sealed class SpriteEdit(string name, LoadedArchive archive, ArchiveEntry entry, Jonbin original, byte[] originalBytes)
    {
        public string Name => name;

        public LoadedArchive Archive => archive;

        public ArchiveEntry Entry => entry;

        public Jonbin Original { get; } = original;

        public byte[] OriginalBytes { get; } = originalBytes;

        public int Capacity { get; } = originalBytes.Length + ExtraBoxes * Jonbin.BoxLength;

        public Jonbin Current { get; private set; } = original;

        public byte[] CurrentBytes { get; private set; } = originalBytes;

        public byte[] Saved { get; set; } = originalBytes;

        public uint? Buffer { get; set; }

        public string? SavePath { get; set; }

        public bool IsChanged => !CurrentBytes.AsSpan().SequenceEqual(OriginalBytes);

        public bool IsUnsaved => !CurrentBytes.AsSpan().SequenceEqual(Saved);

        public void Show(Jonbin jonbin, byte[] bytes)
        {
            Current = jonbin;
            CurrentBytes = bytes;
        }
    }
}
