namespace CSScriptingLang.Parsing.AST;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class ASTNodeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class VisitableNodePropertyAttribute : Attribute { }

public partial interface IAstVisitor { }

public partial class BaseAstVisitor : IAstVisitor
{
    protected HashSet<BaseNode> _visitedNodes = new();
}