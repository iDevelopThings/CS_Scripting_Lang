using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class ObjectProperty : BaseNode
{
    public string Name { get; set; }

    [VisitableNodeProperty]
    public Expression Value { get; set; }

    public ObjectProperty(string name, Expression value) {
        Name  = name;
        Value = value;
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        foreach (var resolveType in Value.ResolveAndCacheTypes(ctx, symbol)) {
            yield return resolveType;
        }
    }
}

[ASTNode]
public partial class ObjectLiteralExpression : LiteralValueExpression
{
    public override bool IsConstant => true;

    [VisitableNodeProperty]
    public List<ObjectProperty> Properties { get; } = new();

    public ObjectLiteralExpression(object value = null) : base(value) { }

    public ObjectProperty AddProperty(string name, Expression value) {
        var prop = new ObjectProperty(name, value);
        Properties.Add(prop);
        return prop;
    }

    public override ITypeAlias GetTypeAlias() => TypeAlias<ObjectPrototype>.Get();

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        var ty       = Ty.Object();
        
        foreach (var prop in Properties) {
            var val = prop.ResolveAndCacheTypes(ctx, symbol).First();
            ty.SetMember(prop.Name, val);
        }
        
        yield return ty;
    }

    public override ValueReference Execute(ExecContext ctx) {
        var obj = Value.Object(Properties.Select(p => {
            var val = p.Value.Execute(ctx);
            return (p.Name, val.Value);
        }), ctx);

        return ctx.ValReference(obj);
    }
}