using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Modules;

public class ModuleScriptAstData
{
    public ModuleDeclarationNode ModuleDeclaration { get; set; }
    public ImportStatementsNode  ImportStatements  { get; set; }

    public string ModuleName => ModuleDeclaration?.Name.NativeValue;

    public bool HasMainFunction => Program?.Cursor.All.Of<FunctionDeclaration>().Any(func => func.Name.ToLower() == "main") ?? false;

    public ProgramExpression Program { get; set; }

    public bool DidParseImports { get; set; }

    public LexerTokenStream Lexer;
    public StandaloneParser Parser;
}

public class ModuleScriptDeclarations
{
    public virtual HashSet<ITopLevelDeclarationNode> TopLevelDeclarations { get; set; } = new();

    public virtual HashSet<RuntimeTypeInfo_Struct>      StructTypes             { get; set; } = new();
    public virtual HashSet<RuntimeType>                 InterfaceTypes          { get; set; } = new();
    public virtual HashSet<RuntimeTypeInfo_Signal>      SignalTypes             { get; set; } = new();
    public virtual HashSet<Value>                       FunctionTypes           { get; set; } = new();
    public virtual HashSet<DefDeclaration_FunctionNode> DefFunctionDeclarations { get; set; } = new();

    public virtual HashSet<VariableSymbol> VariableDeclarations { get; set; } = new();
}

public class Script
{
    public const string Extension  = ".js";
    public const string LanguageId = "csscripting";

    public int Id { get; set; }

    public InterpreterFile File { get; set; }

    public string DirectoryPath        => File.Dir;
    public string FilePath             => File.Abs;
    public string RelativePath         => File.Rel;
    public string Source               => File.Content;
    public string Name                 => File.Name;
    public string NameWithoutExtension => Path.GetFileNameWithoutExtension(Name);

    public Module Module { get; set; }

    public ModuleScriptAstData AstData { get; set; } = new();

    public bool        IsMain  => AstData.HasMainFunction;
    public ProgramExpression Program => AstData.Program;

    public ModuleScriptDeclarations Declarations = new();

    public Script(InterpreterFile file, int id) {
        File = file;
        Id   = id;
    }

    public ProgramExpression Parse() {
        AstData.Parser = new StandaloneParser(this);
        return AstData.Program = AstData.Parser.Parse();
    }

    public void RegisterDeclarations(ExecContext ctx) {
        var visitor = new ScriptDeclarationVisitor(ctx, this);
        Program.Accept(visitor);

        Module.Declarations.Add(Declarations);
    }
    public int AppendInput(string input) {
        if (string.IsNullOrEmpty(input) || input == "") {
            return 0;
        }

        var addedStatements = AstData.Parser.AppendInput(input);
        return addedStatements;
    }
}