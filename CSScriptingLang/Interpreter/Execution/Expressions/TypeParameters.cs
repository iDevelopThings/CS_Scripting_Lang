using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class TypeParameterNode : IdentifierExpression
{
    public TypeParameterNode() { }
    public TypeParameterNode(string name) : base(name) { }

}

[ASTNode]
public partial class TypeParametersListNode : NodeList<TypeParameterNode> { }