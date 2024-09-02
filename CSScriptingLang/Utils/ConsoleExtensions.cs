using Alba.CsConsoleFormat;

namespace CSScriptingLang.Utils;

public static class ConsoleExtensions
{
    /*public static ConsoleSpan Bold(this string      @this) => new($"\x1b[1m{@this}\x1b[0m");
    public static ConsoleSpan Bold(this ConsoleSpan @this) => @this.Text.Bold();
    public static Span        Bold(this Span        @this) => @this.Text.Bold();

    public static ConsoleSpan Underline(this string      @this) => new($"\x1b[4m{@this}\x1b[0m");
    public static ConsoleSpan Underline(this ConsoleSpan @this) => @this.Text.Underline();
    public static Span        Underline(this Span        @this) => @this.Text.Underline();

    public static ConsoleSpan Italic(this string      @this) => new($"\x1b[3m{@this}\x1b[0m");
    public static ConsoleSpan Italic(this ConsoleSpan @this) => @this.Text.Italic();
    public static Span        Italic(this Span        @this) => @this.Text.Italic();

    public static ConsoleSpan Colored(this string      @this, ConsoleColor color) =>
        new(@this) {
            Color = color
        };
    public static ConsoleSpan Colored(this ConsoleSpan @this, ConsoleColor color) => @this.Text.Colored(color);
    public static Span Colored(this Span @this, ConsoleColor color) {
        @this.Color = color;
        return @this;
    }*/
}

public class ConsoleSpan : Span
{
    public ConsoleSpan(string text) : base(text) { }

    public static implicit operator ConsoleSpan(string text) => new(text);
    public static implicit operator string(ConsoleSpan span) => span.Text;
}