namespace BBTAG_IM.Core.Scripting;

public readonly record struct ScriptValue(int Number, byte[]? Text)
{
    public bool IsText => Text is not null;

    public static ScriptValue Of(int number) => new(number, null);

    public static ScriptValue Of(byte[] text) => new(0, text);

    public bool Equals(ScriptValue other) =>
        Number == other.Number && (Text is null ? other.Text is null : other.Text is not null && Text.AsSpan().SequenceEqual(other.Text));

    public override int GetHashCode() => Text is null ? Number.GetHashCode() : Text.Length;
}
