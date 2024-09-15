using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Parsing.AST;
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
}

[ASTNode]
public partial class ObjectLiteralExpression : LiteralValueExpression
{
    public override bool IsConstant => true;

    [VisitableNodeProperty]
    public List<ObjectProperty> Properties { get; } = new();

    public RuntimeTypeInfo_Object ObjectType { get; set; }

    public ObjectLiteralExpression(object value = null) : base(value) { }

    public ObjectProperty AddProperty(string name, Expression value) {
        var prop = new ObjectProperty(name, value);
        Properties.Add(prop);
        return prop;
    }

    public override ValueReference Execute(ExecContext ctx) {
        var obj = Value.Object(Properties.Select(p => {
            var val   = p.Value.Execute(ctx);
            return (p.Name, val.Value);
        }));
        
        return ctx.ValReference(obj);
    }
}

