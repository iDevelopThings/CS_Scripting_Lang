using System.Diagnostics;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
[DebuggerDisplay("Identifier: {Name}")]
public partial class IdentifierExpression : Expression, IExecutableNode
{
    public string Name  { get; set; }
    public bool   IsRef { get; set; }

    public IdentifierExpression() { }
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
        
        if (ctx.TryGetVariable(Name, out var variable)) {
            return ctx.VariableAccessReference(variable);
        }
        
        if (ctx.ModuleResolver.TryGet(Name, out var module)) {
            DiagnosticManager.Diagnostic_Error_Fatal().Message($"Not implemented: Module access '{Name}'").Range(this).Report();
        }
        
        DiagnosticManager.Diagnostic_Error_Fatal().Message($"Variable '{Name}' not found").Range(this).Report();

        return new ValueReference();
    }
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if (Scope.TryGetSymbol(Name, out var def)) {
            foreach (var t in def.Types) {
                yield return t;
            }
            
            yield break;
        }
        
        DiagnosticManager.Diagnostic_Warning().Message($"Could not resolve type for {Name}").Range(this).Report();
    }
    
    
    public override IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
        var s = scope ?? Scope;
        if (s.TryGetSymbol(Name, out var def)) {
            return def.Named;
        }
        return ResolvedTypes.SelectMany(type => type.NamedSymbols);
    }

}

[ASTNode]
[DebuggerDisplay("TypeIdentifier: {Name}")]
public partial class TypeIdentifierExpression : IdentifierExpression
{
    [VisitableNodeProperty]
    public TypeParametersList TypeParameters { get; set; }

    public TypeIdentifierExpression() { }
    public TypeIdentifierExpression(string name) : base(name) { }


    public static TypeIdentifierExpression Unit() => new("Unit");

    public ITypeAlias ResolveType() {
        return TypeAlias.Get(Name);
    }
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if (TypesTable.GetPrototypeTypeByName(Name, out var type)) {
            yield return type.PrototypeInstance.Ty;
            yield break;
        }
        
        DiagnosticManager.Diagnostic_Warning().Message($"Could not resolve type for {Name}").Range(this).Report();
    }
}