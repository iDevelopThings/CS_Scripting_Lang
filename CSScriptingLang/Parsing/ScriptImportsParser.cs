using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing;

public class ScriptImportsParser : ParserBase
{
    public ScriptImportsParser(Script script) {
        Script = script;
        Lexer        = new LexerTokenStream(script.Source);
    }

    public void Parse() {
        try {
            if (Token.IsModuleKeyword) {
                Script.AstData.ModuleDeclaration = ParseModuleDeclaration();
            }

            if (Token.IsImportKeyword) {
                Script.AstData.ImportStatements = ParseImportStatements();
            }

            Script.AstData.DidParseImports = true;
            Script.AstData.Lexer   = Lexer;
        }
        catch (SyntaxException e) {
            Logger.Exception(e);
        }
    }

    public static ScriptImportsParser Parse(Script script) {
        var parser = new ScriptImportsParser(script);
        parser.Parse();
        return parser;
    }
}