namespace CSScriptingLang.Parsing.AST.NamedSymbol;

public interface INamedSymbolProvider
{
    public IEnumerable<NamedSymbolInformation> GetNamedSymbols();
}