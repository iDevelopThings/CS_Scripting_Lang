using System.Collections.Immutable;
using System.Text;
using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using Engine.Engine.Logging;
using PrettyPrompt;
using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;
using PrettyPrompt.Documents;
using PrettyPrompt.Highlighting;
using AnsiConsole = Spectre.Console.AnsiConsole;

namespace CSScriptingLang.Interpreter.REPL;

public class ReplHistory
{
    public List<string> History { get; }      = new();
    public int          Index   { get; set; } = -1;

    public ReplHistory() {
        LoadFromDisk();
    }

    public void Add(string input) {
        History.Add(input);
        Index = History.Count;

        WriteToDisk();
    }

    public string GetNext() {
        if (Index < History.Count - 1) {
            Index++;
        }

        return Index < History.Count ? History[Index] : null;
    }

    public string GetPrevious() {
        if (Index > 0) {
            Index--;
        }

        return Index >= 0 ? History[Index] : null;
    }

    public void Clear() {
        History.Clear();
        Index = -1;

        WriteToDisk();
    }

    public bool IsEmpty() => History.Count == 0;
    public bool IsAtEnd() => Index == History.Count - 1;

    public void WriteToDisk() {
        File.WriteAllLines(Path.Combine(Environment.CurrentDirectory, "repl_history.txt"), History);
    }

    public void LoadFromDisk() {
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "repl_history.txt"))) {
            var lines = File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "repl_history.txt"));
            foreach (var line in lines) {
                var l = line.Trim();
                if (!string.IsNullOrWhiteSpace(l)) {
                    History.Add(l);
                }
            }

            Index = History.Count;
        }
    }
}

public static class SymbolCompletion
{
    public static List<VariableSymbol> All         { get; set; }
    public static List<CompletionItem> Completions { get; set; }
}

public class ReplPromptCallbacks : PromptCallbacks
{
    protected override IEnumerable<(KeyPressPattern Pattern, KeyPressCallbackAsync Callback)> GetKeyPressCallbacks() {

        yield return (
            new(ConsoleModifiers.Control, ConsoleKey.B),
            ShowDoc
        );

    }


    private static Task<KeyPressCallbackResult> ShowDoc(string text, int caret, CancellationToken cancellationToken) {
        string wordUnderCursor = GetWordAtCaret(text, caret).ToLower();

        AnsiConsole.WriteLine($"Showing documentation for {wordUnderCursor}");


        // since we return a null KeyPressCallbackResult here, the user will remain on the current prompt
        // and will still be able to edit the input.
        // if we were to return a non-null result, this result will be returned from ReadLineAsync(). This
        // is useful if we want our custom keypress to submit the prompt and control the output manually.
        return Task.FromResult<KeyPressCallbackResult>(null);

        // local functions
        static string GetWordAtCaret(string text, int caret) {
            var    words        = text.Split(new[] {' ', '\n'});
            string wordAtCaret  = string.Empty;
            int    currentIndex = 0;
            foreach (var word in words) {
                if (currentIndex < caret && caret < currentIndex + word.Length) {
                    wordAtCaret = word;
                    break;
                }

                currentIndex += word.Length + 1; // +1 due to word separator
            }

            return wordAtCaret;
        }
    }

    protected override Task<bool> ShouldOpenCompletionWindowAsync(string text, int caret, KeyPress keyPress, CancellationToken cancellationToken) {
        if (caret > 0 && text[caret - 1] is '.' or '(') // typical "intellisense behavior", opens for new methods and parameters
        {
            return Task.FromResult(true);
        }

        var lastFourChars = text.AsSpan(Math.Max(0, caret - 4), Math.Min(4, caret)).ToString();
        if ("var".StartsWith(lastFourChars, StringComparison.OrdinalIgnoreCase) || "var".EndsWith(lastFourChars, StringComparison.OrdinalIgnoreCase)) {
            return Task.FromResult(true);
        }

        if (caret == 1 && !char.IsWhiteSpace(text[0])                           // 1 word character typed in brand new prompt
                       && (text.Length == 1 || !char.IsLetterOrDigit(text[1]))) // if there's more than one character on the prompt, but we're typing a new word at the beginning (e.g. "a| bar")
        {
            return Task.FromResult(true);
        }

        // open when we're starting a new "word" in the prompt.
        if (caret - 2 >= 0 && char.IsWhiteSpace(text[caret - 2]) && char.IsLetter(text[caret - 1])) {
            return Task.FromResult(true);
        }


        return Task.FromResult(false);
    }

    protected override Task<bool> ConfirmCompletionCommit(string text, int caret, KeyPress keyPress, CancellationToken cancellationToken) {
        return base.ConfirmCompletionCommit(text, caret, keyPress, cancellationToken);
    }

    protected override Task<(IReadOnlyList<OverloadItem>, int ArgumentIndex)> GetOverloadsAsync(string text, int caret, CancellationToken cancellationToken) {
        return base.GetOverloadsAsync(text, caret, cancellationToken);
    }

    protected override Task<TextSpan> GetSpanToReplaceByCompletionAsync(string text, int caret, CancellationToken cancellationToken) {
        return base.GetSpanToReplaceByCompletionAsync(text, caret, cancellationToken);
    }

    protected override Task<IReadOnlyList<CompletionItem>> GetCompletionItemsAsync(string text, int caret, TextSpan spanToBeReplaced, CancellationToken cancellationToken) {
        // demo completion algorithm callback
        // populate completions and documentation for autocompletion window
        var typedWord  = text.AsSpan(spanToBeReplaced.Start, spanToBeReplaced.Length).ToString();
        var typedWords = text.Split(new[] {' ', '\n', '.'}).Where(w => string.IsNullOrWhiteSpace(w) == false).ToArray();
        // get index of `typedWord` in `typedWords`
        var currentWordIndex = Array.IndexOf(typedWords, typedWord);
        // get the word before `typedWord`
        var previousWord = currentWordIndex > 0 ? typedWords[currentWordIndex - 1] : string.Empty;

        var isVarLookup = "var".StartsWith(typedWord, StringComparison.OrdinalIgnoreCase)
                       || "var".StartsWith(typedWord, StringComparison.OrdinalIgnoreCase)
                       || "var".StartsWith(previousWord, StringComparison.OrdinalIgnoreCase)
                       || "var".EndsWith(previousWord, StringComparison.OrdinalIgnoreCase);


        static CharacterSetModificationRule CreateCommitRuleForUserKeybinding(in KeyPressPatterns commitCompletion) {
            var alwaysCommitCharacters = commitCompletion.DefinedPatterns?.Select(key => key.Character).ToArray() ?? [];
            return new CharacterSetModificationRule(CharacterSetModificationKind.Add, ImmutableArray.Create(alwaysCommitCharacters));
        }

        SymbolCompletion.All = ReplProcessor.Ctx.Variables
           .AsParallel()
           .SelectMany(scope => scope.Table.Values.AsParallel())
           .Where(s => {
                if (s.Name.StartsWith(typedWord, StringComparison.OrdinalIgnoreCase))
                    return true;
                return isVarLookup;
            })
           .ToList();

        FormattedString GetRuntimeValueFormatted(string name, Value value) {
            var valueString = value.Inspect(false);
            var parts = new List<FormattedString>() {
                new(name, new FormatSpan(0, name.Length, AnsiColor.White)),
                new($"({value.Type.Name()})", new FormatSpan(0, value.Type.Name().Length + 2, AnsiColor.Blue)),
                // new(" = ", new FormatSpan(0, 3, AnsiColor.White)),
                // new(valueString, new FormatSpan(0, valueString.Length, AnsiColor.Blue))
            };

            var outFormat = new FormattedString("");
            foreach (var formattedString in parts) {
                outFormat += formattedString;
            }

            return outFormat;
        }

        SymbolCompletion.Completions = SymbolCompletion.All.AsParallel()
           .Select(symbol => {
                return new CompletionItem(
                    replacementText: symbol.Name,
                    displayText: symbol.Name,
                    commitCharacterRules: [CreateCommitRuleForUserKeybinding(ReplProcessor.KeyBindings.CommitCompletion)],
                    filterText: symbol.Name,
                    getExtendedDescription: _ => {
                        return Task.FromResult(GetRuntimeValueFormatted(symbol.Name, symbol.Val));
                        // return Task.FromResult(new FormattedString(symbol.Name, new FormatSpan(0, symbol.Name.Length, AnsiColor.Red)));
                    });
            })
           .ToList();


        if (text.EndsWith(".")) {
            try {
                var parser = new StandaloneParser(text);
                parser.Parse();
                var propAccess = parser.Program.Cursor.All.Of<MemberAccessExpression>().ToList().LastOrDefault();
                if (propAccess != null) {
                    var access = ReplProcessor.Interpreter.Execute(propAccess, ReplProcessor.Ctx);
                    if (access.HasValues()) {
                        var vals = access.GetValues();
                    }
                }
            }
            catch (Exception e) {
                ReplProcessor.console.WriteError(e.Message);
            }

            var            lastWord  = typedWords[^1];
            var            firstWord = typedWords[0];
            VariableSymbol symbol    = null;
            if (!ReplProcessor.Ctx.Variables.Get(firstWord, out symbol)) {
                ReplProcessor.Ctx.Variables.Get(lastWord, out symbol);
            }

            var stringPath = "";
            // join all words except for the first by a '.', for ex first = `a`, 2nd = `b`, 3rd = `c`, path = `b.c`
            if (typedWords.Length > 1) {
                stringPath = string.Join(".", typedWords.Skip(1));
            }

            if (symbol != null) {
                var rtValue = symbol.Val;
                if (typedWords.Length > 1) {
                    rtValue = symbol.Val.GetMemberByPath(stringPath);
                }

                if (rtValue != null) {
                    SymbolCompletion.Completions = rtValue.Members.Select((pair) => {
                        var name  = pair.Key;
                        var value = pair.Value;

                        return new CompletionItem(
                            replacementText: name,
                            displayText: name,
                            commitCharacterRules: [CreateCommitRuleForUserKeybinding(ReplProcessor.KeyBindings.CommitCompletion)],
                            filterText: name,
                            getExtendedDescription: _ => {
                                return Task.FromResult(GetRuntimeValueFormatted(name, value));
                            });
                    }).ToList();
                }

            }
        }


        return Task.FromResult<IReadOnlyList<CompletionItem>>(
            SymbolCompletion.Completions
        );
    }


    private static readonly (string Name, AnsiColor Color)[] ColorsToHighlight = new[] {
        ("import", AnsiColor.White),
        ("if", AnsiColor.Blue),
        ("else", AnsiColor.White),
        ("while", AnsiColor.White),
        ("for", AnsiColor.BrightBlue),
        ("function", AnsiColor.BrightBlue),
        ("return", AnsiColor.White),
        ("var", AnsiColor.White),
        ("range", AnsiColor.BrightBlue),
        ("defer", AnsiColor.BrightBlue),
        ("async", AnsiColor.BrightBlue),
        ("await", AnsiColor.BrightBlue),
        ("coroutine", AnsiColor.BrightBlue),
        ("yield", AnsiColor.BrightBlue),
        ("signal", AnsiColor.BrightBlue),
        ("true", AnsiColor.Blue),
        ("false", AnsiColor.Blue),
    };

    protected override Task<IReadOnlyCollection<FormatSpan>> HighlightCallbackAsync(string text, CancellationToken cancellationToken) {
        // demo syntax highlighting callback

        IReadOnlyCollection<FormatSpan> spans = EnumerateFormatSpans(text, ColorsToHighlight).ToList();
        return Task.FromResult(spans);
    }

    private static IEnumerable<FormatSpan> EnumerateFormatSpans(string text, IEnumerable<(string TextToFormat, AnsiColor Color)> formattingInfo) {
        foreach (var (textToFormat, color) in formattingInfo) {
            int startIndex;
            int offset = 0;
            while ((startIndex = text.AsSpan(offset).IndexOf(textToFormat)) != -1) {
                yield return new FormatSpan(offset + startIndex, textToFormat.Length, color);
                offset += startIndex + textToFormat.Length;
            }
        }
    }
}

public class ReplProcessor
{
    public static SystemConsole console;
    public static InterpreterFile  file;


    public static Interpreter  Interpreter;
    public static ExecContext  Ctx;

    public static Script Script;
    public static Module       Module;

    public static KeyBindings KeyBindings { get; set; }
    public static ReplHistory History = new();

    public ReplProcessor(Interpreter interpreter) {
        Interpreter = interpreter;
        Ctx         = Interpreter.GetNewExecContext();

        file = Interpreter.FileSystem.AddFile("main.js");

        Interpreter.ModuleResolver.Load(Ctx);

        Ctx = Interpreter.Initialize();

        Module = Interpreter.ModuleResolver.Get("main");
        Module.IsMainModule = true;

        Script = Module.GetScriptByName("main.js");
        
        Interpreter.ModuleResolver.SetMainScript(Script);
    }

    private string ReadInputWithHistory() {
        var input          = new StringBuilder();
        var cursorPosition = 0;

        while (true) {
            // var info = Console.ReadKey();
            var key = Console.ReadKey(intercept: true); // Read the key without printing

            switch (key.Key) {
                case ConsoleKey.LeftArrow: {
                    if (cursorPosition <= 0) {
                        Console.SetCursorPosition(cursorPosition + 2, Console.CursorTop);
                        break;
                    }

                    cursorPosition--;
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    break;
                }
                case ConsoleKey.RightArrow: {
                    if (cursorPosition >= input.Length) {
                        Console.SetCursorPosition(cursorPosition + 2, Console.CursorTop);
                        break;
                    }

                    cursorPosition++;
                    Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                    break;
                }

                case ConsoleKey.Enter: {
                    var inputStr = input.ToString().Trim();
                    if (inputStr.Length > 0) {
                        History.Add(inputStr);

                        return input.ToString();
                    }

                    break;
                }
                case ConsoleKey.UpArrow: {
                    var prev = History.GetPrevious();
                    if (prev != null) {
                        ClearCurrentConsoleLine();
                        input.Clear();
                        input.Append(prev);
                        RedrawInput(input.ToString(), input.Length);
                    }

                    cursorPosition = input.Length;

                    break;
                }
                case ConsoleKey.DownArrow when !History.IsAtEnd(): {
                    var next = History.GetNext();
                    if (next != null) {
                        ClearCurrentConsoleLine();
                        input.Clear();
                        input.Append(next);
                        RedrawInput(input.ToString(), input.Length);
                    }

                    cursorPosition = input.Length;

                    break;
                }
                case ConsoleKey.DownArrow:
                    History.Index = History.History.Count;
                    ClearCurrentConsoleLine();
                    input.Clear();
                    cursorPosition = 0;
                    RedrawInput(input.ToString(), cursorPosition);
                    break;

                case ConsoleKey.Backspace when input.Length > 0: {
                    input.Remove(cursorPosition - 1, 1);
                    cursorPosition--;
                    RedrawInput(input.ToString(), cursorPosition);

                    // ClearCurrentConsoleLine();
                    // Console.Write($"> {input}");

                    break;
                }
                default:
                    input.Insert(cursorPosition, key.KeyChar);
                    cursorPosition++;
                    RedrawInput(input.ToString(), cursorPosition);
                    break;
            }
        }
    }
    private void RedrawInput(string input, int cursorPosition) {
        Console.SetCursorPosition(0, Console.CursorTop);                  // Move to the beginning of the line
        Console.Write(new string(' ', Console.WindowWidth));              // Clear the line
        Console.SetCursorPosition(0, Console.CursorTop);                  // Move to the beginning again
        Console.Write($"> {input}");                                      // Write the new input
        Console.SetCursorPosition(cursorPosition + 2, Console.CursorTop); // Reposition the cursor
    }
    private void ClearCurrentConsoleLine() {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop);
    }

    public async Task Start() {
        AnsiConsole.WriteLine("Welcome to the REPL. Type 'exit' to quit.");

        // if (!History.IsEmpty()) {
        //     ProcessInput(History.History.Last());
        // }


        console = new SystemConsole();

        KeyBindings = new KeyBindings(
            commitCompletion: new KeyPressPatterns([
                new(' '),
                new('{'),
                new('}'),
                new('['),
                new(']'),
                new('('),
                new(')'),
                new('.'),
                new(','),
                new(':'),
                new(';'),
                new('+'),
                new('-'),
                new('*'),
                new('/'),
                new('%'),
                new('&'),
                new('|'),
                new('^'),
                new('!'),
                new('~'),
                new('='),
                new('<'),
                new('>'),
                new('?'),
                new('@'),
                new('#'),
                new('\''),
                new('"'),
                new('\\'),
                new(ConsoleKey.Enter),
                new(ConsoleKey.Tab),
            ])
        );

        var prompt = new Prompt(
            console: console,
            persistentHistoryFilepath: "./history-file",
            callbacks: new ReplPromptCallbacks(),
            configuration: new PromptConfiguration(
                prompt: new FormattedString("> ", new FormatSpan(0, 1, AnsiColor.BrightBlack)),
                completionItemDescriptionPaneBackground: AnsiColor.Rgb(30, 30, 30),
                selectedCompletionItemBackground: AnsiColor.Rgb(30, 30, 30),
                selectedTextBackground: AnsiColor.Rgb(20, 61, 102),
                keyBindings: KeyBindings
            )
        );

        while (true) {
            var response = await prompt.ReadLineAsync();
            if (response.IsSuccess) {
                if (response.Text == "exit") break;
                // optionally, use response.CancellationToken so the user can
                // cancel long-running processing of their response via ctrl-c

                ProcessInput(response.Text);
                AnsiConsole.WriteLine();

                console.WriteLine("You wrote " + (response.SubmitKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) ? response.Text.ToUpper() : response.Text));
                // AnsiConsole.WriteLine("You wrote " + (response.SubmitKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) ? response.Text.ToUpper() : response.Text));
            }
        }


        /*
        while (true) {

            Console.Write("> "); // REPL prompt

            var input = ReadInputWithHistory();
            if (string.IsNullOrWhiteSpace(input)) {
                continue;
            }

            if (input == "exit") {
                break; // Exit the REPL
            }

            ProcessInput(input);
        }

        Console.WriteLine("Goodbye!");*/
    }


    private void ProcessInput(string input) {
        ExecResult result;

        /*try {*/


        if (string.IsNullOrWhiteSpace(input) || input == "\b") {
            return;
        }

        var newStatements = Script.AppendInput(input);
        if (newStatements == 0) {
            return;
        }

        var newNodes = Script.Program.Nodes.Skip(Script.Program.Nodes.Count - newStatements).ToList();

        try {
            using var _ = Logs.TempConsumer(message => {
                /*var canIntercept = message.Severity < LogLevel.Error;
                // if (canIntercept) {
                //     canIntercept = message.Logger.Type != typeof(PrintFunctions);
                // }

                ClearCurrentConsoleLine();

                if (canIntercept) {
                    Console.WriteLine($"> {input.BoldBrightGray()}{" -> ".BrightGray()}{message.Message}");
                } else {
                    Console.WriteLine($"> {input.BoldBrightGray()}");
                }

                input = "";


                return canIntercept;*/
                return true;
            });

            result = Interpreter.ExecuteNodes(newNodes, Ctx);

            // if (result.HasValues()) {
            //     foreach (var value in result.GetValues()) {
            //         if (value is RuntimeValue rtv) {
            //             Console.WriteLine(rtv.Inspect());
            //         } else {
            //             Console.WriteLine(value);
            //         }
            //     }
            // }
        }
        catch (CompilationException e) {
            Console.WriteLine(e.Message);
        }


        /*}
        catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}"); // Print any errors that occur during execution
            Console.WriteLine(ex.StackTrace);
        }*/
    }
}