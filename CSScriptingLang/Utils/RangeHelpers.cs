using System.Collections.Immutable;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace CSScriptingLang.Utils;

public static class SourceHelpers
{
    public static bool IsMultiLine(this string source) => source.Contains('\n');
    
    public static Position GetPositionAtIndex(string source, int index) {
        var line         = 0;
        var span         = source.AsSpan();
        var rollingIndex = 0;
        do {
            var location = span.IndexOf('\n');
            if (location == -1) {
                if (rollingIndex + span.Length >= index) {
                    return new Position(line, index - rollingIndex);
                }

                return (line, span.Length);
            }

            if (rollingIndex + location >= index) {
                return new Position(line, index - rollingIndex);
            }

            span         =  span.Slice(location + 1);
            rollingIndex += location + 1;
            line++;
            if (rollingIndex == index) {
                return new Position(line, 0);
            }
        } while (!span.IsEmpty);

        return (line, 0);
    }

    public static string NormalizeLineEndings(this string value) => value.Replace("\r\n", "\n");

    public static string InsertAtRange(this SourceWrapper source, Range range, string text) {
        var start = source.GetIndexAtPosition(range.Start);
        var end   = source.GetIndexAtPosition(range.End);
        return source.Source.Substring(0, start) + text + source.Source.Substring(end);
    }
    public static string ExtractRange(this SourceWrapper source, Range range) {
        var start = source.GetIndexAtPosition(range.Start);
        return source.Source.Substring(start, source.GetIndexAtPosition(range.End) - start);
    }

    public static IEnumerable<string> ExtractRanges(this SourceWrapper source, IEnumerable<Range> ranges) {
        foreach (var range in ranges) {
            var start = source.GetIndexAtPosition(range.Start);
            yield return source.Source.Substring(start, source.GetIndexAtPosition(range.End) - start);
        }
    }

    public static int GetIndexAtPosition(IReadOnlyList<string> lines, Position position) {
        if (position.Line >= lines.Count) return -1;
        var characterCount = lines
           .Take(position.Line)
           .Aggregate(0, (acc, v) => acc + v.Length);
        return characterCount + position.Character;
    }

    public static int GetIndexAtPosition(in string[] lines, Position position) {
        if (position.Line >= lines.Length) return -1;
        var characterCount = lines
           .Take(position.Line)
           .Aggregate(0, (acc, v) => acc + v.Length);
        return characterCount + position.Character;
    }
}

public struct SourceWrapper
{
    private string _source;

    public string Source {
        get => _source;
        set {
            _source = value;
            Lines   = [..ParseLines(value)];
        }
    }
    public ImmutableArray<string> Lines  { get; set; }

    public SourceWrapper(string source) {
        Source = source;
    }

    private static IEnumerable<string> ParseLines(string source) {
        var lastStart = 0;
        var length    = source.Length;
        for (var index = 0; index < length; index++) {
            if (source[index] == '\n') {
                yield return source.Substring(lastStart, Math.Min(index + 1, length) - lastStart);
                lastStart = index + 1;
            }
        }

        yield return source.Substring(lastStart);
    }

    public Position GetPositionAtIndex(int      index)    => SourceHelpers.GetPositionAtIndex(Source, index);
    public int      GetIndexAtPosition(Position position) => SourceHelpers.GetIndexAtPosition(Lines, position);
}