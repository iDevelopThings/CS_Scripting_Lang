using CSScriptingLang.Core;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.IncrementalParsing;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.Interpreter.Modules;

public class Script
{
    private static Logger Logger = Logs.Get<Script>();

    public const  string               Extension  = ".vlt";
    public const  string               LanguageId = "voltum";
    public static TextDocumentSelector LanguageSelector => TextDocumentSelector.ForLanguage(LanguageId);

    public int Id { get; set; }

    public InterpreterFile File { get; set; }

    public DocumentUri Uri => DocumentUri.FromFileSystemPath(File.Abs);

    public string DirectoryPath => File.Dir;
    public string FilePath      => File.Abs;
    public string RelativePath  => File.Rel;
    public string Source        => IsWrappedModule ? WrappedSource : File.Content;

    public string WrappedSource => $$"""
                                     function module(object exports, string fileName, string dirName) {
                                     {{File.Content}}
                                     }
                                     """.NormalizeLineEndings();

    public string Name                 => File.Name;
    public string NameWithoutExtension => Path.GetFileNameWithoutExtension(Name);

    public Module Module { get; set; }

    public SourceText SourceText => new(Source);

    public bool IsMain {
        get {
            if (Program != null) {
                return Program?.AllOfType<FunctionDeclaration>().Any(func => func.Name.ToLower() == "main") ?? false;
            }
            if (SyntaxTree != null) {
                return SyntaxTree.SyntaxRoot.HasChildElement<FunctionDecl>(
                    func => func.Name.Equals("main", StringComparison.CurrentCultureIgnoreCase)
                );
            }
            return false;
        }
    }
    // public ProgramExpression Program => AstData.Program;

    public ModuleScriptDeclarations Declarations = new();
    public NamedSymbols             NamedSymbols => ModuleResolver.NamedSymbols[RelativePath];

    public LexerTokenStream  Lexer   { get; set; }
    public Parser            Parser  { get; set; }
    public ProgramExpression Program { get; set; }

    public LexerTokenStream IncrementalLexer { get; set; }
    public SyntaxTree       SyntaxTree       { get; set; }

    public Value Exports { get; set; }

    // When false, this will be a script, when true, it's a function with our script inside
    public bool IsWrappedModule { get; set; }
    public bool IsEntryModule   { get; set; }

    public Action<Script> OnVersionChanged { get; set; }


    private ExecContext _ctx;
    public Script(InterpreterFile file, int id) {
        File = file;
        Id   = id;

        File.PropertyChanged += (sender, args) => {
            if (args.PropertyName == nameof(InterpreterFile.Version)) {
                OnVersionChanged?.Invoke(this);
            }
        };
        File.OnChanged += (f) => {
            ReparseScript();

            /*CreateLexer();
            if (InterpreterConfig.ExecMode == InterpreterExecMode.IncrementalSyntaxTree) {
                SyntaxTree = SyntaxTree.ParseText(this);
            }
            if (InterpreterConfig.ExecMode == InterpreterExecMode.Original) {
                if (Parser != null) {
                    Parser = new Parser(this);
                    if (IsEntryModule) {
                        Program          = Parser.Parse();
                        Program.IsModule = false;
                        RegisterDeclarations(_ctx);
                    } else {
                        Program          = Parser.ParseModule();
                        Program.IsModule = true;
                        RegisterDeclarations(_ctx);
                    }
                }
            }*/
        };
    }

    public void ReparseScript() {
        if (InterpreterConfig.ExecMode == InterpreterExecMode.IncrementalSyntaxTree) {
            IncrementalLexer = new LexerTokenStream(
                Source,
                token => {
                    token.ScriptId = Id;
                },
                true,
                LexerState.CanOutputComments | LexerState.CanOutputNewLines | LexerState.CanOutputWhitespace
            );

            SyntaxTree = SyntaxTree.ParseText(this, !IsWrappedModule);

            RegisterDeclarations(_ctx);

            return;
        }

        Lexer = new LexerTokenStream(
            Source,
            token => {
                token.ScriptId = Id;
            }
        );

        Parser           = new Parser(this);
        Program          = IsWrappedModule ? Parser.ParseModule() : Parser.Parse();
        Program.IsModule = IsWrappedModule;
        RegisterDeclarations(_ctx);

        /*
        if (IsWrappedModule) {
            Program          = Parser.Parse();
            Program.IsModule = false;
            RegisterDeclarations(_ctx);
        } else {
            Program          = Parser.ParseModule();
            Program.IsModule = true;
            RegisterDeclarations(_ctx);
        }*/
    }

    public ProgramExpression ParseWrappedModule(ExecContext ctx) {
        _ctx = ctx;

        ReparseScript();

        // CreateLexer();
        // SyntaxTree       = SyntaxTree.ParseText(this);
        // Parser           = new Parser(this);
        // Program          = Parser.ParseModule();
        // Program.IsModule = true;
        // RegisterDeclarations(ctx);

        return Program;
    }
    public ProgramExpression ParseEntryModule(ExecContext ctx) {
        _ctx = ctx;

        ReparseScript();

        // CreateLexer();
        // SyntaxTree = SyntaxTree.ParseText(this);
        // Parser  = new Parser(this);
        // Program = Parser.Parse();
        // Program.IsModule = false;
        // RegisterDeclarations(ctx);

        return Program;
    }

    public void RegisterDeclarations(ExecContext ctx) {
        ModuleResolver.NamedSymbols.ClearScriptSymbols(RelativePath);

        if (Program != null) {
            var visitor = new ScriptDeclarationVisitor(ctx, this);
            Program.Accept(visitor);
        }

        if (SyntaxTree != null) {
            var visitor = new DeclarationVisitor(ctx, this);
            SyntaxTree.SyntaxRoot.Accept(visitor);
            visitor.FinalizeLazyDeclarations();
        }
    }
    public int AppendInput(string input) {
        if (string.IsNullOrEmpty(input) || input == "") {
            return 0;
        }

        var addedStatements = Parser.AppendInput(input);
        return addedStatements;
    }

    public int GetCol(int    offset)        => SourceText.GetCol(offset);
    public int GetLine(int   offset)        => SourceText.GetLine(offset);
    public int GetOffset(int line, int col) => SourceText.GetOffset(line, col);

}