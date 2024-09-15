using System.Runtime.CompilerServices;
using System.Text;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

public enum FatalErrorHandlingMethodType
{
    ThrowException,
    Exit,
}

public class ErrorWriter
{
    public static Caller Caller { get; set; }

    private static string ScriptSource { get; set; }
    private static string ScriptName   { get; set; }
    private static string ScriptPath   { get; set; }

    public static FatalErrorHandlingMethodType FatalErrorHandlingMethod { get; set; } = FatalErrorHandlingMethodType.ThrowException;

    public static void SetScriptIfUndefined(Script script) {
        if (string.IsNullOrWhiteSpace(ScriptPath) && script != null) {
            SetScriptInfo(script);
        }
    }
    public static void SetScriptInfo(Script script) {
        if (script == null)
            return;

        ScriptPath   = script.FilePath;
        ScriptName   = script.File.Name;
        ScriptSource = script.Source;
    }
    public static void Configure(Script script, string file, int line, string member)
        => Configure(script, Caller.FromAttributes(file, line, member));
    public static void Configure(Script script, Caller caller) {
        SetScriptInfo(script);
        Caller = caller;
    }

    public static ErrorWriter Create() => new ErrorWriter();
    public static ErrorWriter Create(Script script, Caller caller) {
        Caller = caller;
        SetScriptInfo(script);

        return new ErrorWriter();
    }
    public static ErrorWriter Create(Script script, string file, int line, string member)
        => Create(script, Caller.FromAttributes(file, line, member));

    public static ErrorWriter Create(BaseNode node, Caller caller)
        => Create(node?.GetScript(), caller);
    public static ErrorWriter Create(BaseNode node, string file, int line, string member)
        => Create(node, Caller.FromAttributes(file, line, member));

    public static ErrorWriter Create(string source, Caller caller) {
        Caller       = caller;
        ScriptSource = source;

        return new ErrorWriter();
    }


    public ErrorWriter SetSourceIfNull(string newInput) {
        if (string.IsNullOrWhiteSpace(ScriptSource))
            ScriptSource = newInput;

        return this;
    }


    public void TryFatalExit(string message, bool logStack = false) {
        if (logStack) {
            var callers = CallerList.Get(3, 10);
            message += "\n" + string.Join("\n", callers);
        }

        if (FatalErrorHandlingMethod == FatalErrorHandlingMethodType.ThrowException)
            throw new CompilationException(message);

        Console.WriteLine(message);
        Environment.Exit(1);
    }

    public void LogFatal(string message, TokenRange from, TokenRange to) {
        var sb = LogError(message, from, to);
        TryFatalExit(sb.ToString());
    }

    public void LogFatal(string message, BaseNode node, bool logStack = false) {
        var sb = LogError(message, node);
        TryFatalExit(sb.ToString(), logStack);
    }
    public StringBuilder LogError(string message, BaseNode node) {
        if (node == null) {
            return LogError(message, new TokenRange(), new TokenRange());
        }

        var (from, to) = node.Cursor.FindTokenRange();

        return LogError(message, from.Range, to.Range);
    }

    public void LogWarning(string message, BaseNode node) {
        var (from, to) = node.Cursor.FindTokenRange();
        var sb = LogError(message, from.Range, to.Range);
        Console.WriteLine(sb.ToString());
    }
    public StringBuilder LogError(string message, TokenRange from, TokenRange to) {
        var (start, end) = GetLineAndColumn(from.Start, to.End);

        var startLine   = start.Line;
        var startColumn = start.Column;

        var endLine   = end.Line;
        var endColumn = end.Column;

        var output = new StringBuilder();

        output.AppendLine(new string('-', 80));
        output.AppendLine($"Error: {message}");
        if (ScriptPath != null) {
            output.AppendLine($"{ScriptPath}:{startLine}:{startColumn}");
        } else {
            output.AppendLine($"Location: {startLine}:{startColumn}");
        }

        if (Caller.IsValid()) {
            output.AppendLine($"{Caller.File}:{Caller.Line} -> {Caller.MethodFullName}");
        }

        output.AppendLine();

        PrintSourceWithHighlight(output, startLine, startColumn, endLine, endColumn, message);

        output.AppendLine(new string('-', 80));

        return output;
    }
    public StringBuilder LogError(string message, Token startToken, Token endToken) {
        var (startLine, startColumn) = GetLineAndColumn(startToken);
        var (endLine, endColumn)     = GetLineAndColumn(endToken);

        var output = new StringBuilder();

        output.AppendLine(new string('-', 80));
        output.AppendLine($"Error: {message}");
        output.AppendLine($"Location: Line {startLine}, Column {startColumn} to Line {endLine}, Column {endColumn}");
        output.AppendLine();

        PrintSourceWithHighlight(output, startLine, startColumn, endLine, endColumn, message);

        output.AppendLine(new string('-', 80));

        return output;
    }

    struct TokenLineColumn
    {
        public int Position { get; set; }
        public int Line     { get; set; }
        public int Column   { get; set; }
    }

    private (TokenLineColumn, TokenLineColumn) GetLineAndColumn(int startPosition, int endPosition) {
        var curLine   = 1;
        var curColumn = 1;

        var start = new TokenLineColumn() {Position = startPosition};
        var end   = new TokenLineColumn() {Position = endPosition};

        for (var i = 0; i < endPosition; i++) {
            if (i == startPosition) {
                start.Line   = curLine;
                start.Column = curColumn;
            }

            if (endPosition >= ScriptSource.Length) {
                end.Line   = curLine;
                end.Column = curColumn;
                break;
            }

            if (ScriptSource[i] == '\n') {
                curLine++;
                curColumn = 1;
            } else {
                curColumn++;
            }


        }

        end.Line   = curLine;
        end.Column = curColumn;

        return (start, end);
    }

    private (int line, int column) GetLineAndColumn(Token token) {
        var line      = 1;
        var column    = 1;
        var charCount = 0;

        for (var i = 0; i < token.Value.Length; i++) {
            if (ScriptSource[charCount] == '\n') {
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
        var lines = ScriptSource.Split('\n');

        var startingLine = Math.Max(0, startLine - 6);
        var endingLine   = Math.Min(lines.Length, endLine + 6);

        var isMultiLineError = startLine != endLine;
        // find the max str length over the range of lines
        var maxStrLength = 0;
        for (var i = startingLine; i < endingLine; i++) {
            maxStrLength = Math.Max(maxStrLength, lines[i].Length);
        }

        maxStrLength += 4;
        maxStrLength =  Math.Max(maxStrLength, message.Length + 4);


        for (var i = startingLine; i < endingLine; i++) {
            var lineNumber = i + 1;
            var line       = lines[i];

            var isErrLine = lineNumber >= startLine && lineNumber <= endLine;

            void writeLineNumber(bool isCodeLine = true) {
                if (!isCodeLine) {
                    output.Append($"{".",4} | ".BrightGray());
                } else {
                    output.Append($"{lineNumber,4} | ".BrightGray());
                }
            }

            string writeLineNumberStr(bool isCodeLine = true) {
                if (!isCodeLine) {
                    return $"{".",4} | ";
                } else {
                    return $"{lineNumber,4} | ";
                }
            }

            if (lineNumber == startLine && lineNumber == endLine) {
                // Error range within the same line
                writeLineNumber();
                output.Append(line.Substring(0, startColumn - 1));
                output.Append(line.Substring(startColumn - 1, Math.Max(1, endColumn - startColumn)).BoldBrightRed());
                output.AppendLine(line.Substring(Math.Max(line.Length, endColumn - 1)));
                PrintHighlightLine(output, startColumn, endColumn - startColumn, message);
            } else if (lineNumber == startLine) {
                // Start of the error range

                if (isMultiLineError) {
                    writeLineNumber(false);
                    output.AppendLine($"/{new string('-', maxStrLength - 2)}\\".BrightGray());
                }

                writeLineNumber();
                output.Append($"| {new string(' ', 2)}".BrightGray());
                output.Append(line.Substring(0, startColumn - 1));
                output.Append(line.Substring(startColumn - 1).TrimEnd().BrightRed());
                output.Append($"{new string(' ', Math.Max(0, maxStrLength - (line.Length + 4)))}|".BrightGray());
                output.AppendLine();

                if (!isMultiLineError) {
                    PrintHighlightLine(output, startColumn, line.Length - startColumn + 1, message);
                }

            } else if (lineNumber == endLine) {
                // End of the error range

                writeLineNumber();

                output.Append($"| {new string(' ', 2)}".BrightGray());
                output.Append(line.Substring(0, endColumn - 1).BrightRed());
                output.Append(line.Substring(endColumn - 1).TrimEnd());
                output.Append($"{new string(' ', Math.Max(0, maxStrLength - (line.Length + 4)))}|".BrightGray());
                output.AppendLine();

                if (!isMultiLineError) {
                    PrintHighlightLine(output, 1, endColumn - 1, message);
                } else {
                    output.Append($"{writeLineNumberStr(false)}|{new string('-', maxStrLength - 2)}|\n".BrightGray());

                    PrintHighlightLine(output, 1, endColumn - 1, message);

                    output.Append($"{writeLineNumberStr(false)}\\{new string('-', maxStrLength - 2)}/\n".BrightGray());
                }
            } else if (lineNumber > startLine && lineNumber < endLine) {
                // Entire line is within the error range
                var lnStr      = writeLineNumberStr(false);
                var prefixLine = $"| {new string(' ', 2)}";
                line = line.TrimEnd();

                output.Append(lnStr.BrightGray());
                output.Append(prefixLine.BrightGray());
                output.Append(line.BrightRed());
                output.Append($"{new string(' ', Math.Max(0, maxStrLength - (line.Length + prefixLine.Length + 1)))}|".BrightGray());
                output.AppendLine();
                // output.AppendLine($"{lnStr.BrightGray()}{prefixLine.BrightGray()}{line.BrightRed()}{new string(' ', Math.Max(0, maxStrLength - totalLen))} |".BrightGray());

                // PrintHighlightLine(output, 1, line.Length, message);
            } else {
                // Normal line without error

                writeLineNumber();
                output.Append(line);
                output.AppendLine();
            }
        }
    }

    private void PrintHighlightLine(StringBuilder output, int startColumn, int length, string message) {

        output.Append($"{".",4} | ".BrightGray());
        output.Append(new string(' ', startColumn - 1));
        int numCarets = Math.Max(1, length - 1);
        output.Append(new string('^', 1).BoldBrightRed());
        if (numCarets > 1) {
            output.Append(new string('~', numCarets - 1).BrightRed());
        }

        output.AppendLine($"{"^".BoldBrightRed()} {message}");
    }
}

public class CompilationException : Exception
{
    public CompilationException(string message) : base(message) { }
}

public class BaseLanguageException : Exception
{
    public Caller Caller { get; set; }

    public string Input { get; set; }

    public CallerList Trace { get; set; }

    private Script _script;
    public Script Script {
        get => _script;
        set {
            _script = value;
            if (string.IsNullOrWhiteSpace(Input) && value != null) {
                Input = value.Source;
            }
        }
    }


    public BaseLanguageException(string message) : base(message) {
        Caller = Caller.FromException(this);
    }
    public BaseLanguageException(string message, Caller caller) : base(message) {
        Caller = caller;
    }
}

public class BaseLanguageException<T> : BaseLanguageException where T : BaseLanguageException<T>
{
    public BaseLanguageException(string message) : base(message) { }
    public BaseLanguageException(string message, Caller caller) : base(message, caller) { }

    public T WithCaller([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        Caller = Caller.FromAttributes(file, line, member);
        return (T) this;
    }
    public T WithCaller(Caller caller) {
        Caller = caller;
        return (T) this;
    }

    public T WithInput(string input) {
        Input = input;
        return (T) this;
    }

}

public class DeclarationException : BaseLanguageException<DeclarationException>
{
    public BaseNode Node { get; set; }
    public DeclarationException(string message, RuntimeType type) : base(message) {
        Node   = type.LinkedNode;
        Script = type.DeclarationContext?.DeclaringScript;
    }
    public DeclarationException(string message, BaseNode node, Script script) : base(message) {
        Node   = node;
        Script = script;
    }
}

public class InterpreterException : BaseLanguageException<InterpreterException>
{
    public BaseNode Node { get; set; }

    public InterpreterException(string message, BaseNode node) : base(message) {
        Node   = node;
        Script = node?.GetScript();
    }
    public InterpreterException(string message, BaseNode node, Script script) : base(message) {
        Node   = node;
        Script = script;
    }
}

public class FatalInterpreterException : InterpreterException
{
    public FatalInterpreterException(string message, BaseNode node) : base(message, node) {
        Trace = CallerList.Get(1, 8);
    }

    public FatalInterpreterException(string message, BaseNode node, Script script) : base(message, node, script) { }
}

public class SyntaxException : BaseLanguageException<SyntaxException>
{
    public Token      Token { get; set; }
    public TokenRange Range { get; set; }

    public SyntaxException(string message, TokenRange range) : base(message) {
        Range = range;
    }
    public SyntaxException(string message, Token token) : base(message) {
        Token = token;
        Range = token.Range;
    }
}

public class ParserException : BaseLanguageException<ParserException>
{
    public TokenRange From { get; set; }
    public TokenRange To   { get; set; }

    public ParserException(string message, TokenRange from, TokenRange to, Script script) : base(message) {
        From   = from;
        To     = to;
        Script = script;
    }
}

public class LexerException : BaseLanguageException<LexerException>
{
    public TokenRange From { get; set; }
    public TokenRange To   { get; set; }

    public LexerException(string message, TokenRange from, TokenRange to) : base(message) {
        From = from;
        To   = to;
    }
}

public class InterpreterRuntimeException : BaseLanguageException<InterpreterRuntimeException>
{
    public InterpreterRuntimeException(string message) : base(message) { }
}

public class FailedToGetRuntimeTypeException : InterpreterException
{
    public FailedToGetRuntimeTypeException(BaseNode node, string message) : base($"Failed to get runtime type for node {node.GetType().ToShortName()}: {message}", node) { }
}