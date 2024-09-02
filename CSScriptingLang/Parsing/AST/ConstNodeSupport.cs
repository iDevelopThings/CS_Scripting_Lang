using CSScriptingLang.Lexing;
using CSScriptingLang.VM;

namespace CSScriptingLang.Parsing.AST;

public interface IConstantNode
{
    public object UntypedValue { get; }

    
}
