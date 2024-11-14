using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.Parsing.AST.NamedSymbol;

public enum NamedSymbolKind
{
    None = 0,
    Variable,
    Function,
    Struct,
    StructMethod,
    StructField,
    Interface,
    Enum,
    EnumMember,
}

public static class NamedSymbolKindExtensions
{
    public static SymbolKind ToSymbolKind(this NamedSymbolKind kind) {
        return kind switch {
            NamedSymbolKind.None         => SymbolKind.Null,
            NamedSymbolKind.Variable     => SymbolKind.Variable,
            NamedSymbolKind.Function     => SymbolKind.Function,
            NamedSymbolKind.Struct       => SymbolKind.Struct,
            NamedSymbolKind.StructMethod => SymbolKind.Method,
            NamedSymbolKind.StructField  => SymbolKind.Field,
            NamedSymbolKind.Interface    => SymbolKind.Interface,
            NamedSymbolKind.Enum         => SymbolKind.Enum,
            NamedSymbolKind.EnumMember   => SymbolKind.EnumMember,
            _                            => SymbolKind.Null,
        };
    }
}