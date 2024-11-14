namespace CSScriptingLang.IncrementalParsing;

public readonly record struct SourceRange
{
    public static SourceRange Empty = new();
    public        int         EndOffset   => StartOffset + Length;
    public        int         StartOffset { get; init; }
    public        int         Length      { get; init; }

    public SourceRange(int StartOffset = 0, int Length = 0) {
        this.StartOffset = StartOffset;
        this.Length      = Length;
    }

    public override string ToString() => $"[{StartOffset}..{EndOffset})";

    public bool Contains(int         offset) => offset >= StartOffset && offset < EndOffset;
    public bool Contains(SourceRange range)  => range.StartOffset >= StartOffset && range.EndOffset <= EndOffset;

    public bool Intersects(SourceRange range) => StartOffset < range.EndOffset && range.StartOffset < EndOffset;

    public SourceRange Merge(SourceRange range) {
        var start = Math.Min(StartOffset, range.StartOffset);
        var end   = Math.Max(EndOffset, range.EndOffset);
        return new SourceRange(start, end - start);
    }

    public static implicit operator SourceRange((int start, int length) range) => new(range.start, range.length);
}