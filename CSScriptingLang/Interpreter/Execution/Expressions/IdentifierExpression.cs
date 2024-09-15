using System.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
[DebuggerDisplay("Identifier: {Name}")]
public partial class IdentifierExpression : Expression, IExecutableNode
{
    public string Name  { get; set; }
    public bool   IsRef { get; set; }

    public IdentifierExpression(){}
    public IdentifierExpression(string name) {
        Name = name;
    }

    public static implicit operator string(IdentifierExpression ident) => ident.Name;

    private Value cachedStringVal;
    public static implicit operator Value(IdentifierExpression ident) {
        return ident.cachedStringVal ?? (ident.cachedStringVal = Value.String(ident.Name));
    }

    public IdentifierExpression AsRef(bool isRef) {
        IsRef = isRef;
        return this;
    }

    public override ValueReference Execute(ExecContext ctx) {
        if (ctx.ModuleResolver.TryGet(Name, out var module)) {
            throw new InterpreterException($"Not implemented: Module access '{Name}'", this);
        }
        if (ctx.Variables.Get(Name, out var variable)) {
            return ctx.VariableAccessReference(variable);
        }

        throw new InterpreterException($"Variable '{Name}' not found", this);
    }
}