using System.Numerics;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Selection;
using BBTAG_IM.ImprovementMod.Dev;
using BBTAG_IM.ImprovementMod.Training;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class HitboxEditorWindow(
    GameView game,
    IGameCollision collision,
    HitboxEdits edits,
    DevScripts scripts,
    MatchPause? pause,
    SpritePreview preview,
    DevFolder dev) : IMenuWindow
{
    private const string Title = "Hitbox editor";
    private const string SpriteOnScreen = "Sprite on screen";
    private const int NothingSelected = -1;
    private const int NoMove = -1;
    private const float FitContent = 0;
    private const float CanvasWidthInFontSizes = 20;
    private const float CanvasHeightInFontSizes = 26;
    private const float MoveWidthInFontSizes = 16;
    private const float FrameWidthInFontSizes = 12;
    private const float ValueWidthInFontSizes = 3.5f;
    private const float GridStep = 50;
    private const float BoxFill = 0.25f;
    private const float Outline = 1.5f;
    private const float SelectedOutline = 3f;
    private const float HandleSide = 4f;
    private const float OriginArm = 8f;
    private const float DragSpeed = 1f;
    private const float NoLimit = 0;
    private const string WholeNumbers = "%.0f";
    private const string WidestBoxName = "Hurtbox 99";
    private const string RemoveLabel = "Remove";
    private const uint CanvasBackground = 0xFF201A1A;
    private const uint GridColor = 0x30FFFFFF;
    private const uint GroundColor = 0x90FFFFFF;
    private const uint OriginColor = 0xFF00FFFF;

    private static readonly Vector2 FirstPosition = new(0.3f, 0.05f);
    private static readonly JonbinBox NewHurtbox = new((int)BoxKind.Hurtbox, -50, -250, 100, 250);
    private static readonly JonbinBox NewHitbox = new((int)BoxKind.Hitbox, -120, -250, 100, 80);

    private readonly SpriteBreakpoint _breakpoint = new();
    private readonly MoveTab _moves = new("Moves", "Move", effects: false);
    private readonly MoveTab _effects = new("Effects", "Effect", effects: true);
    private readonly Dictionary<IReadOnlyList<CharacterMove>, IReadOnlyList<string>> _names = new(ReferenceEqualityComparer.Instance);

    private bool _isOpen;
    private int _slot;
    private bool _follow = true;
    private bool _pauseOnFrame;
    private string? _target;
    private int _selected = NothingSelected;
    private BoxGrab? _grab;
    private string _notice = string.Empty;
    private bool _wasInMatch;

    public void Show() => _isOpen = true;

    public void OnPresent(nint device)
    {
        var inMatch = game.State == GameState.Match;

        if (inMatch && !_wasInMatch)
            StartOver();

        _wasInMatch = inMatch;

        if (!_pauseOnFrame || _target is not { } target || !game.InTrainingMatch || game.Character(_slot) is not { } entity)
        {
            _breakpoint.Reached(showing: false);
            return;
        }

        if (_breakpoint.Reached(ShownSprites.Of(game, entity).Any(sprite => sprite.Name == target)))
            pause?.PauseNow();
    }

    public void Draw(Vector2 displaySize)
    {
        if (!_isOpen)
            return;

        ImGui.SetNextWindowPos(displaySize * FirstPosition, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(FitContent), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(Title, ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            DrawContents();

        ImGui.End();
    }

    private void StartOver()
    {
        edits.Reset();
        preview.Forget();
        _names.Clear();
        ForgetMoves();
        _target = null;
        _notice = string.Empty;
    }

    private void ForgetMoves()
    {
        _moves.Forget();
        _effects.Forget();
        _selected = NothingSelected;
        _grab = null;
    }

    private void DrawContents()
    {
        if (!game.InTrainingMatch)
        {
            MenuText.Muted("Start a training match to change the boxes.");
            return;
        }

        if (ChosenCharacter() is not { } character)
            return;

        if (game.Character(character.Slot) is not { } entity || EntitySprite.Read(game.Memory, entity) is not { } shown)
        {
            MenuText.Muted("This character shows no sprite right now.");
            return;
        }

        DrawPlayback();

        if (ImGui.BeginTabBar("lists"))
        {
            DrawTab(_moves, character, entity, shown);
            DrawTab(_effects, character, entity, shown);
            ImGui.EndTabBar();
        }

        DrawSave();

        if (_notice.Length > 0)
            MenuText.Wrapped(_notice);
    }

    private DevCharacter? ChosenCharacter()
    {
        var characters = scripts.Characters();

        if (characters.Count == 0)
            return null;

        var chosen = characters.FirstOrDefault(character => character.Slot == _slot) ?? characters[0];
        _slot = chosen.Slot;
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * MoveWidthInFontSizes);

        if (ImGui.BeginCombo("Character", LabelOf(chosen)))
        {
            foreach (var character in characters)
            {
                if (ImGui.Selectable(LabelOf(character), character == chosen) && character != chosen)
                    Choose(character.Slot);

                MenuCombo.KeepSelectedInView(character == chosen);
            }

            ImGui.EndCombo();
        }

        var position = characters.ToList().IndexOf(chosen);
        var next = WheelChoice.Next(position, characters.Count, MenuCombo.WheelSteps());

        if (next != position)
            Choose(characters[next].Slot);

        return chosen;
    }

    private void Choose(int slot)
    {
        _slot = slot;
        ForgetMoves();
    }

    private void DrawPlayback()
    {
        if (ImGui.Checkbox("Follow Animation", ref _follow) && _follow)
            _pauseOnFrame = false;

        MenuText.Tooltip("Shows the move and frame the character is in, or the effect it has out. Picking a move or a frame turns it off.");
        ImGui.SameLine();

        if (ImGui.Checkbox("Pause on this frame", ref _pauseOnFrame) && _pauseOnFrame)
            _follow = false;

        MenuText.Tooltip("Pauses the match when the character, or one of its effects, reaches the frame shown here. Do the move and it stops on that frame.");
    }

    private void DrawTab(MoveTab tab, DevCharacter character, uint entity, EntitySprite shown)
    {
        if (!ImGui.BeginTabItem(tab.Title))
            return;

        var moves = scripts.Moves(character, tab.Effects);

        if (_follow && _grab is null)
            FollowAnimation(tab, moves, entity, shown);

        if (ChosenMove(tab, moves, shown) is { } move)
        {
            DrawMoveChoice(tab, moves);
            DrawFrameChoice(tab, move);
            DrawFrame(new EditTarget(character, entity, shown, move, tab.Frame));
        }

        ImGui.EndTabItem();
    }

    private void FollowAnimation(MoveTab tab, IReadOnlyList<CharacterMove> moves, uint entity, EntitySprite shown)
    {
        var found = MoveFollowing.Find(moves, Showing(tab, entity, shown), tab.Position);

        if (found is null && tab.Effects)
            return;

        if (found == tab.Position && (found is not null || tab.FollowedSprite == shown.Name))
            return;

        tab.Position = found;
        tab.FollowedSprite = shown.Name;
        _selected = NothingSelected;
    }

    private IEnumerable<ShownState> Showing(MoveTab tab, uint entity, EntitySprite shown)
    {
        if (!tab.Effects)
            return [new ShownState(EntitySprite.StateOf(game.Memory, entity) ?? string.Empty, shown.Name)];

        return ShownSprites.Of(game, entity)
            .Where(sprite => sprite.Entity != entity)
            .Select(sprite => new ShownState(EntitySprite.StateOf(game.Memory, sprite.Entity) ?? string.Empty, sprite.Name));
    }

    private static CharacterMove? ChosenMove(MoveTab tab, IReadOnlyList<CharacterMove> moves, EntitySprite shown)
    {
        if (tab.Position is { } position && position.Move < moves.Count)
            return Clamped(tab, moves[position.Move]);

        if (!tab.Effects)
        {
            tab.Position = null;
            return new CharacterMove(SpriteOnScreen, [new MoveFrame(shown.Name, 0)]);
        }

        if (moves.Count == 0)
        {
            MenuText.Muted("This character has no effects in its script.");
            return null;
        }

        tab.Position = new MovePosition(0, 0);
        return moves[0];
    }

    private static CharacterMove Clamped(MoveTab tab, CharacterMove move)
    {
        if (tab.Position is { } position && position.Frame >= move.Frames.Count)
            tab.Position = position with { Frame = move.Frames.Count - 1 };

        return move;
    }

    private void DrawMoveChoice(MoveTab tab, IReadOnlyList<CharacterMove> moves)
    {
        var chosen = tab.Search.Draw(tab.Label, NamesOf(moves), tab.Position?.Move ?? NoMove, SpriteOnScreen, ImGui.GetFontSize() * MoveWidthInFontSizes);

        if (chosen is { } index)
            ShowFrame(tab, new MovePosition(index, 0));

        if (moves.Count == 0)
            MenuText.Muted("The moves could not be read from the character's script, so only the sprite on screen can be changed.");
    }

    private IReadOnlyList<string> NamesOf(IReadOnlyList<CharacterMove> moves)
    {
        if (_names.TryGetValue(moves, out var known))
            return known;

        IReadOnlyList<string> names = [.. moves.Select(move => move.Name)];
        _names[moves] = names;
        return names;
    }

    private void DrawFrameChoice(MoveTab tab, CharacterMove move)
    {
        var count = move.Frames.Count;
        var current = tab.Frame;
        var moveIndex = tab.Position?.Move ?? NoMove;

        if (ImGui.ArrowButton("previous", ImGuiDir.Left))
            ShowFrame(tab, new MovePosition(moveIndex, (current - 1 + count) % count));

        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * FrameWidthInFontSizes);
        var number = current + 1;

        if (ImGui.SliderInt("##frame", ref number, 1, count, $"Frame %d / {count}"))
            ShowFrame(tab, new MovePosition(moveIndex, number - 1));

        ImGui.SameLine();

        if (ImGui.ArrowButton("next", ImGuiDir.Right))
            ShowFrame(tab, new MovePosition(moveIndex, (current + 1) % count));

        var frame = move.Frames[current];
        ImGui.SameLine();
        ImGui.TextUnformatted(frame.Duration > 0 ? $"{frame.Sprite}, {frame.Duration} {(frame.Duration == 1 ? "frame" : "frames")}" : frame.Sprite);
    }

    private void ShowFrame(MoveTab tab, MovePosition position)
    {
        tab.Position = position.Move == NoMove ? null : position;
        _follow = false;
        _selected = NothingSelected;
        _grab = null;
    }

    private void DrawFrame(EditTarget target)
    {
        _target = target.Frame.Sprite;

        if (!target.Frame.ShowsASprite)
        {
            MenuText.Muted("This frame shows no sprite, so it has no boxes.");
            return;
        }

        try
        {
            var jonbin = edits.Read(target.Sprite);
            var showing = ShownSprites.Of(game, target.Entity).FirstOrDefault(sprite => sprite.Name == target.Sprite.Name);
            ImGui.TextUnformatted($"{(showing is null ? "Not on screen now" : "On screen now")}{(edits.IsChanged(target.Sprite) ? ", changed" : string.Empty)}");
            DrawCanvas(target, jonbin);
            ImGui.SameLine();
            ImGui.BeginGroup();
            DrawBoxTable(target, jonbin);
            DrawButtons(target, jonbin);
            ImGui.EndGroup();

            if (showing is not null)
                DrawOnGame(showing.Entity, jonbin);
        }
        catch (InvalidCollisionDataException exception)
        {
            MenuText.Wrapped(exception.Message);
            _grab = null;
        }
    }

    private void DrawCanvas(EditTarget target, Jonbin jonbin)
    {
        var size = new Vector2(CanvasWidthInFontSizes, CanvasHeightInFontSizes) * ImGui.GetFontSize();
        var topLeft = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton("canvas", size);
        var hovered = ImGui.IsItemHovered();
        var clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        var canvas = _grab?.Canvas ?? BoxCanvas.Fit(MoveBoxes(target), topLeft, size);
        var rectangles = CanvasRectangles(jonbin, canvas);
        var draw = ImGui.GetWindowDrawList();

        draw.PushClipRect(topLeft, topLeft + size, true);
        draw.AddRectFilled(topLeft, topLeft + size, CanvasBackground);
        DrawGrid(draw, canvas, topLeft, size);
        preview.Draw(draw, target.Character, jonbin, canvas);
        DrawBoxes(draw, jonbin, rectangles);
        draw.AddLine(canvas.Origin - new Vector2(OriginArm, 0), canvas.Origin + new Vector2(OriginArm, 0), OriginColor, Outline);
        draw.AddLine(canvas.Origin - new Vector2(0, OriginArm), canvas.Origin + new Vector2(0, OriginArm), OriginColor, Outline);
        draw.PopClipRect();

        HandleMouse(target, jonbin, rectangles, canvas, hovered, clicked);
    }

    private IEnumerable<JonbinBox> MoveBoxes(EditTarget target)
    {
        List<JonbinBox> boxes = [];

        foreach (var frame in target.Move.Frames.Where(frame => frame.ShowsASprite))
        {
            if (edits.Find(target.Shown with { Name = frame.Sprite }) is not { } jonbin)
                continue;

            boxes.AddRange(jonbin.Boxes.Where(box => EditableKind(box) is not null));
            boxes.AddRange(preview.Outline(target.Character, jonbin));
        }

        return boxes;
    }

    private static ScreenRect?[] CanvasRectangles(Jonbin jonbin, BoxCanvas canvas) =>
        [.. jonbin.Boxes.Select(box => EditableKind(box) is null ? (ScreenRect?)null : canvas.ToCanvas(box))];

    private static void DrawGrid(ImDrawListPtr draw, BoxCanvas canvas, Vector2 topLeft, Vector2 size)
    {
        var step = GridStep * canvas.Scale;
        var bottomRight = topLeft + size;

        for (var x = FirstLine(canvas.Origin.X, topLeft.X, step); x < bottomRight.X; x += step)
            draw.AddLine(new Vector2(x, topLeft.Y), new Vector2(x, bottomRight.Y), GridColor);

        for (var y = FirstLine(canvas.Origin.Y, topLeft.Y, step); y < bottomRight.Y; y += step)
            draw.AddLine(new Vector2(topLeft.X, y), new Vector2(bottomRight.X, y), GridColor);

        draw.AddLine(new Vector2(topLeft.X, canvas.Origin.Y), new Vector2(bottomRight.X, canvas.Origin.Y), GroundColor);
    }

    private static float FirstLine(float origin, float start, float step) => origin - MathF.Floor((origin - start) / step) * step;

    private void DrawBoxes(ImDrawListPtr draw, Jonbin jonbin, ScreenRect?[] rectangles)
    {
        for (var index = 0; index < rectangles.Length; index++)
        {
            if (rectangles[index] is not { } rectangle)
                continue;

            var color = HitboxColors.Of((BoxKind)jonbin.Boxes[index].Type);
            var min = new Vector2(rectangle.Left, rectangle.Top);
            var max = new Vector2(rectangle.Right, rectangle.Bottom);
            var selected = index == _selected;
            draw.AddRectFilled(min, max, OverlayColors.SwapRedAndBlue(HitboxColors.WithAlpha(color, BoxFill)));
            draw.AddRect(min, max, OverlayColors.SwapRedAndBlue(color), 0, ImDrawFlags.None, selected ? SelectedOutline : Outline);

            if (selected)
                DrawHandles(draw, rectangle, OverlayColors.SwapRedAndBlue(color));
        }
    }

    private static void DrawHandles(ImDrawListPtr draw, ScreenRect rectangle, uint color)
    {
        Vector2[] corners = [new(rectangle.Left, rectangle.Top), new(rectangle.Right, rectangle.Top), new(rectangle.Right, rectangle.Bottom), new(rectangle.Left, rectangle.Bottom)];
        var half = new Vector2(HandleSide);

        foreach (var corner in corners)
            draw.AddRectFilled(corner - half, corner + half, color);
    }

    private void HandleMouse(EditTarget target, Jonbin jonbin, ScreenRect?[] rectangles, BoxCanvas canvas, bool hovered, bool clicked)
    {
        var mouse = ImGui.GetIO().MousePos;

        if (_grab is { } grab)
        {
            Drag(target, jonbin, grab, mouse);
            return;
        }

        if (!hovered)
            return;

        var picked = Pick(rectangles, mouse);

        if (picked is { } hover)
            ImGui.SetMouseCursor(CursorOf(hover.Handle));

        if (!clicked)
            return;

        _selected = picked?.Index ?? NothingSelected;

        if (picked is { } chosen)
            _grab = new BoxGrab(chosen.Index, chosen.Handle, jonbin.Boxes[chosen.Index], mouse, canvas);
    }

    private void Drag(EditTarget target, Jonbin jonbin, BoxGrab grab, Vector2 mouse)
    {
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left) || grab.Index >= jonbin.Boxes.Count)
        {
            _grab = null;
            return;
        }

        ImGui.SetMouseCursor(CursorOf(grab.Handle));
        var moved = BoxDrag.Apply(grab.Start, grab.Handle, mouse - grab.Mouse, grab.Canvas.Axes);

        if (moved != jonbin.Boxes[grab.Index])
            Replace(target, jonbin, grab.Index, moved);
    }

    private (int Index, BoxHandle Handle)? Pick(ScreenRect?[] rectangles, Vector2 mouse)
    {
        if (_selected != NothingSelected && _selected < rectangles.Length && rectangles[_selected] is { } selected && BoxHandles.At(selected, mouse) is not BoxHandle.None and var handle)
            return (_selected, handle);

        for (var index = rectangles.Length - 1; index >= 0; index--)
        {
            if (rectangles[index] is { } rectangle && BoxHandles.At(rectangle, mouse) is not BoxHandle.None and var found)
                return (index, found);
        }

        return null;
    }

    private void DrawBoxTable(EditTarget target, Jonbin jonbin)
    {
        if (!ImGui.BeginTable("boxes", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            return;

        var padding = ImGui.GetStyle().FramePadding.X * 2;
        var value = ImGui.GetFontSize() * ValueWidthInFontSizes;
        ImGui.TableSetupColumn("Box", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(WidestBoxName).X + padding);
        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, value);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, value);
        ImGui.TableSetupColumn("Width", ImGuiTableColumnFlags.WidthFixed, value);
        ImGui.TableSetupColumn("Height", ImGuiTableColumnFlags.WidthFixed, value);
        ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(RemoveLabel).X + padding);
        ImGui.TableHeadersRow();

        for (var index = 0; index < jonbin.Boxes.Count; index++)
        {
            if (EditableKind(jonbin.Boxes[index]) is { } kind)
                DrawBoxRow(target, jonbin, index, kind);
        }

        ImGui.EndTable();
    }

    private void DrawBoxRow(EditTarget target, Jonbin jonbin, int index, BoxKind kind)
    {
        var box = jonbin.Boxes[index];
        ImGui.PushID(index);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        if (ImGui.Selectable($"{NameOf(kind)} {OrdinalOf(jonbin, index) + 1}", _selected == index))
            _selected = index;

        var x = box.X;
        var y = box.Y;
        var width = box.Width;
        var height = box.Height;
        var changed = Value("##x", ref x) | Value("##y", ref y) | Value("##width", ref width) | Value("##height", ref height);

        if (changed)
            Replace(target, jonbin, index, box with { X = x, Y = y, Width = Math.Max(width, BoxDrag.SmallestSide), Height = Math.Max(height, BoxDrag.SmallestSide) });

        ImGui.TableNextColumn();

        if (ImGui.SmallButton(RemoveLabel))
            Remove(target, jonbin, index);

        ImGui.PopID();
    }

    private static bool Value(string label, ref float value)
    {
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * ValueWidthInFontSizes);
        return ImGui.DragFloat(label, ref value, DragSpeed, NoLimit, NoLimit, WholeNumbers);
    }

    private void DrawButtons(EditTarget target, Jonbin jonbin)
    {
        if (ImGui.Button("Add hurtbox"))
            Add(target, jonbin, NewHurtbox);

        ImGui.SameLine();

        if (ImGui.Button("Add hitbox"))
            Add(target, jonbin, NewHitbox);

        ImGui.SameLine();
        ImGui.BeginDisabled(!edits.IsChanged(target.Sprite));

        if (ImGui.Button("Undo changes to this sprite"))
        {
            edits.Undo(target.Sprite);
            _selected = NothingSelected;
        }

        ImGui.EndDisabled();
        MenuText.Muted("Drag a box to move it, or drag its edges to resize it.");
    }

    private void DrawSave()
    {
        var unsaved = edits.Unsaved;
        ImGui.Separator();
        ImGui.BeginDisabled(unsaved.Count == 0);

        if (ImGui.Button("Save"))
            Save();

        ImGui.EndDisabled();
        MenuText.Tooltip($"Writes the changed sprites into {dev.Path}. The game reads them from there in the next matches.");

        if (unsaved.Count > 0)
            MenuText.Muted($"Not saved yet: {string.Join(", ", unsaved)}. Changes that are not saved are lost when the match ends.");
    }

    private void Save()
    {
        try
        {
            var saved = edits.SaveAll();
            _notice = $"Saved {saved.Count} {(saved.Count == 1 ? "sprite" : "sprites")} in {dev.Path}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _notice = $"The boxes could not be saved: {exception.Message}";
        }
    }

    private void DrawOnGame(uint entity, Jonbin jonbin)
    {
        if (_selected == NothingSelected || _selected >= jonbin.Boxes.Count || EditableKind(jonbin.Boxes[_selected]) is not { } kind || game.Camera() is not { } camera)
            return;

        if (collision.Box(entity, kind, OrdinalOf(jonbin, _selected)) is not { } box)
            return;

        var io = ImGui.GetIO();
        var viewport = new Viewport(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
        var rectangle = ScreenRect.Around(ScreenProjection.ToScreen(HitboxGeometry.World(box), camera, viewport));
        var color = OverlayColors.SwapRedAndBlue(HitboxColors.Of(kind));
        var draw = ImGui.GetBackgroundDrawList();
        draw.AddRect(new Vector2(rectangle.Left, rectangle.Top), new Vector2(rectangle.Right, rectangle.Bottom), color, 0, ImDrawFlags.None, SelectedOutline);
        DrawHandles(draw, rectangle, color);
    }

    private void Add(EditTarget target, Jonbin jonbin, JonbinBox box)
    {
        Change(target, [.. jonbin.Boxes, box]);
        _selected = LastOfType(edits.Read(target.Sprite).Boxes, box.Type);
    }

    private void Remove(EditTarget target, Jonbin jonbin, int index)
    {
        Change(target, jonbin.Boxes.Where((_, other) => other != index));
        _selected = NothingSelected;
        _grab = null;
    }

    private void Replace(EditTarget target, Jonbin jonbin, int index, JonbinBox box) =>
        Change(target, jonbin.Boxes.Select((other, position) => position == index ? box : other));

    private void Change(EditTarget target, IEnumerable<JonbinBox> boxes)
    {
        edits.Change(target.Sprite, target.Character.Code, boxes);
        _notice = string.Empty;
    }

    private static int LastOfType(IReadOnlyList<JonbinBox> boxes, int type)
    {
        for (var index = boxes.Count - 1; index >= 0; index--)
        {
            if (boxes[index].Type == type)
                return index;
        }

        return NothingSelected;
    }

    private static int OrdinalOf(Jonbin jonbin, int index) => jonbin.Boxes.Take(index).Count(box => box.Type == jonbin.Boxes[index].Type);

    private static BoxKind? EditableKind(JonbinBox box) => box.Type is (int)BoxKind.Hurtbox or (int)BoxKind.Hitbox ? (BoxKind)box.Type : null;

    private static string NameOf(BoxKind kind) => kind == BoxKind.Hitbox ? "Hitbox" : "Hurtbox";

    private static string LabelOf(DevCharacter character) => $"{DevScripts.SlotName(character.Slot)}: {character.Name}";

    private static ImGuiMouseCursor CursorOf(BoxHandle handle) => handle switch
    {
        BoxHandle.Left or BoxHandle.Right => ImGuiMouseCursor.ResizeEW,
        BoxHandle.Top or BoxHandle.Bottom => ImGuiMouseCursor.ResizeNS,
        BoxHandle.TopLeft or BoxHandle.BottomRight => ImGuiMouseCursor.ResizeNWSE,
        BoxHandle.TopRight or BoxHandle.BottomLeft => ImGuiMouseCursor.ResizeNESW,
        _ => ImGuiMouseCursor.ResizeAll,
    };

    private sealed class MoveTab(string title, string label, bool effects)
    {
        public string Title { get; } = title;

        public string Label { get; } = label;

        public bool Effects { get; } = effects;

        public AutocompleteCombo Search { get; } = new(title);

        public MovePosition? Position { get; set; }

        public string? FollowedSprite { get; set; }

        public int Frame => Position?.Frame ?? 0;

        public void Forget()
        {
            Position = null;
            FollowedSprite = null;
        }
    }

    private sealed record EditTarget(DevCharacter Character, uint Entity, EntitySprite Shown, CharacterMove Move, int FrameIndex)
    {
        public MoveFrame Frame => Move.Frames[FrameIndex];

        public EntitySprite Sprite => Shown with { Name = Frame.Sprite };
    }

    private sealed record BoxGrab(int Index, BoxHandle Handle, JonbinBox Start, Vector2 Mouse, BoxCanvas Canvas);
}
