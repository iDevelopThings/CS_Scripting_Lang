using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

public class ErrorWriter
{
    private readonly string _source;

    public static string CallerFilePath { get; set; } = string.Empty;
    public static int    CallerLine     { get; set; } = 0;
    public static string CallerMethod   { get; set; } = string.Empty;


    public ErrorWriter(string source) {
        _source = source;
    }

    public void LogErrorWithCaller(string message, TokenRange from, TokenRange to, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        CallerFilePath = file;
        CallerLine     = line;
        CallerMethod   = member;

        LogError(message, from, to);

        throw new CompilationException(message);
    }

    public void LogFatal(string message, TokenRange from, TokenRange to) {
        var sb = LogError(message, from, to);
        // Console.WriteLine(sb.ToString());

        throw new CompilationException(sb.ToString());
    }

    public StringBuilder LogError(string message, TokenRange from, TokenRange to) {
        var (startLine, startColumn) = GetLineAndColumn(from.Start);
        var (endLine, endColumn)     = GetLineAndColumn(to.End);

        var output = new StringBuilder();

        output.AppendLine(new string('-', 80));
        output.AppendLine($"Error: {message}");
        output.AppendLine($"Location: {startLine}:{startColumn}");
        if (CallerFilePath.Length > 0 && CallerMethod.Length > 0)
            output.AppendLine($"{CallerFilePath}:{CallerLine} -> {CallerMethod}");
        output.AppendLine();

        PrintSourceWithHighlight(output, startLine, startColumn, endLine, endColumn, message);

        output.AppendLine(new string('-', 80));

        return output;
    }
    public void LogError(string message, Token startToken, Token endToken) {
        var (startLine, startColumn) = GetLineAndColumn(startToken);
        var (endLine, endColumn)     = GetLineAndColumn(endToken);

        var output = new StringBuilder();
        
        output.AppendLine(new string('-', 80));
        output.AppendLine($"Error: {message}");
        output.AppendLine($"Location: Line {startLine}, Column {startColumn} to Line {endLine}, Column {endColumn}");
        output.AppendLine();

        PrintSourceWithHighlight(output, startLine, startColumn, endLine, endColumn, message);

        output.AppendLine(new string('-', 80));
    }

    private (int line, int column) GetLineAndColumn(int position) {
        var line   = 1;
        var column = 1;

        for (var i = 0; i < position; i++) {
            if (_source[i] == '\n') {
                line++;
                column = 1;
            } else {
                column++;
            }

        }

        return (line, column);
    }

    private (int line, int column) GetLineAndColumn(Token token) {
        var line      = 1;
        var column    = 1;
        var charCount = 0;

        for (var i = 0; i < token.Value.Length; i++) {
            if (_source[charCount] == '\n') {
                line++;
                column = 1;
            } else {
                column++;
            }

            charCount++;
        }

        return (line, column);
    }
    private (int line, int column) GetLineAndColumn(TokenRange range) {
        var line      = 1;
        var column    = 1;
        var charCount = 0;

        for (var i = 0; i < range.Total; i++) {
            if (_source[charCount] == '\n') {
                line++;
                column = 1;
            } else {
                column++;
            }

            charCount++;
        }

        return (line, column);
    }


    private void PrintSourceWithHighlight(StringBuilder output, int startLine, int startColumn, int endLine, int endColumn, string message) {
        var lines = _source.Split('\n');

        var startingLine = Math.Max(1, startLine - 4);
        var endingLine   = Math.Min(lines.Length, endLine + 4);

        for (var i = startingLine; i < endingLine; i++) {
            var lineNumber = i + 1;
            var line       = lines[i];

            if (lineNumber == startLine && lineNumber == endLine) {
                // Error range within the same line
                output.Append(line.Substring(0, startColumn - 1));
                output.Append(line.Substring(startColumn - 1, Math.Max(1, endColumn - startColumn)).BoldBrightRed());
                output.AppendLine(line.Substring(Math.Max(line.Length, endColumn - 1)));
                PrintHighlightLine(output, startColumn, endColumn - startColumn, message);
            } else if (lineNumber == startLine) {
                // Start of the error range
                output.Append(line.Substring(0, startColumn - 1));
                output.AppendLine(line.Substring(startColumn - 1).BoldBrightRed());

                PrintHighlightLine(output, startColumn, line.Length - startColumn + 1, message);
            } else if (lineNumber == endLine) {
                // End of the error range

                output.Append(line.Substring(0, endColumn - 1).BoldBrightRed());
                output.AppendLine(line.Substring(endColumn - 1));
                PrintHighlightLine(output, 1, endColumn - 1, message);
            } else if (lineNumber > startLine && lineNumber < endLine) {
                // Entire line is within the error range
                output.AppendLine(line.BoldBrightRed());
                PrintHighlightLine(output, 1, line.Length, message);
            } else {
                // Normal line without error
                output.AppendLine(line);
            }
        }
    }

    private void PrintHighlightLine(StringBuilder output, int startColumn, int length, string message) {
        output.Append(new string(' ', startColumn - 1));
        output.Append(new string('^', Math.Max(1, length)).BoldBrightRed());
        output.AppendLine($" {message}");
    }
}

public class CompilationException : Exception
{
    public CompilationException(string message) : base(message) { }
}