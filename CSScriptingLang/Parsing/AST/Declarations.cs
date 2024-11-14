using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using SharpX;
using Module = CSScriptingLang.Interpreter.Modules.Module;

namespace CSScriptingLang.Interpreter.Execution.Statements { }

namespace CSScriptingLang.Parsing.AST
{
    public interface IDeclarationNode { }

    public class DeclarationContext
    {
        public Module DeclaringModule { get; set; }
        public Script DeclaringScript { get; set; }

        public void Set(Script script) {
            DeclaringScript = script;
            DeclaringModule = script.Module;
        }
    }

    public interface ITopLevelDeclarationNode : IDeclarationNode
    {
        public DeclarationContext DeclarationContext { get; set; }
    }

    [ASTNode]
    public partial class VariableInitializerNode : BaseNode, INamedSymbolProvider
    {
        [VisitableNodeProperty]
        public IdentifierExpression Variable { get; set; }

        public string Name => Variable.Name;

        [VisitableNodeProperty]
        public Expression Val { get; set; }

        public VariableInitializerNode() { }
        public VariableInitializerNode(IdentifierExpression variable, Expression val) {
            Variable = variable;
            Val      = val;

            StartToken = variable.StartToken;
            EndToken   = val.EndToken;
        }

        public ValueReference Execute(ExecContext ctx) {
            if (ctx.Variables.Get(Name, out var symbol)) {
                if (!symbol.IsBaseDeclaration) {
                    ctx.LogError(this, $"Variable '{Name}' already declared");
                }
            }

            if (Val is LiteralValueExpression literal) {
                var literalResult = literal.Execute(ctx);

                var literalSymbol = ctx.Variables.Declare(Name, literalResult.Value);

                ctx.CurrentCallFrame?.Locals.Add(literalSymbol.Name, literalSymbol);

                return ctx.VariableAccessReference(literalSymbol);
            }

            var valueResult = ctx.ExecuteRValue(Val.Execute);

            var varSymbol = ctx.Variables.Set(
                Name, valueResult.Value?.GetOrClone() ?? Value.Null()
            );

            ctx.CurrentCallFrame?.Locals.Add(Name, varSymbol);

            return ctx.VariableAccessReference(varSymbol);
        }

        public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
            Variable.ResolvedTypes = [];

            if (Val is LiteralValueExpression literal) {
                foreach (var t in literal.ResolveAndCacheTypes(ctx, symbol)) {
                    yield return t;
                    Variable.ResolvedTypes.Add(t);
                }

                yield break;
            }

            if (Val == null) {
                throw new FailedToGetRuntimeTypeException(this, $"VariableInitializerNode.Val is null; Variable={Variable.Name}");
            }
            
            foreach (var t in Val.ResolveAndCacheTypes(ctx, symbol)) {
                yield return t;
                Variable.ResolvedTypes.Add(t);
            }
        }

        public IEnumerable<NamedSymbolInformation> GetNamedSymbols() {
            yield return new NamedSymbolInformation(this, Name, NamedSymbolKind.Variable, Variable);
        }

        public override IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
            var s = scope ?? Scope;
            return Val.FindReferences(s);
        }
    }

    [ASTNode]
    public partial class VariableDeclarationNode : Statement, ITopLevelDeclarationNode, INamedSymbolProvider
    {
        public DeclarationContext DeclarationContext { get; set; } = new();

        public TupleListDeclarationNode InitialVariable { get; set; }
        public TupleListDeclarationNode InitialValue    { get; set; }

        public List<VariableInitializerNode> Initializers { get; set; } = new();

        [VisitableNodeProperty]
        public IEnumerable<IdentifierExpression> Variables => Initializers.Select(x => x.Variable);

        [VisitableNodeProperty]
        public IEnumerable<Expression> Values => Initializers.Select(x => x.Val);


        public IEnumerable<string> VariableNames => Variables.Select(x => x.Name);

        public bool HasInitializer<T>() where T : BaseNode {
            return Initializers.Any(x => x.Val is T);
        }

        public VariableDeclarationNode() { }

        public void AddPairs(BaseNode varNodes, BaseNode exprNodes) {
            if (InitialValue == null && varNodes is TupleListDeclarationNode initVariable) {
                InitialVariable = initVariable;
            }

            if (InitialValue == null && exprNodes is TupleListDeclarationNode initValue) {
                InitialValue = initValue;
            }

            {
                if (varNodes is ExpressionList varList) {
                    if (exprNodes is ExpressionList exprList) {
                        for (var i = 0; i < varList.Expressions.Count; i++) {
                            AddPair((IdentifierExpression) varList.Expressions[i], exprList.Expressions[i]);
                        }
                    }

                    if (exprNodes is RangeExpression range) {
                        for (var i = 0; i < varList.Expressions.Count; i++) {
                            AddPair((IdentifierExpression) varList.Expressions[i], range);
                        }
                    }
                    return;
                }
            }
            {
                if (varNodes is TupleListDeclarationNode varList) {
                    if (exprNodes is TupleListDeclarationNode exprList) {
                        for (var i = 0; i < varList.Nodes.Count; i++) {
                            AddPair(varList.Nodes[i] as IdentifierExpression, exprList.Nodes[i] as Expression);
                        }

                        return;
                    }

                    if (exprNodes is ExpressionList exprs) {
                        for (var i = 0; i < varList.Nodes.Count; i++) {
                            AddPair(varList.Nodes[i] as IdentifierExpression, exprs.Expressions[i]);
                        }

                        return;
                    }

                    if (exprNodes is Expression nodes) {
                        for (var i = 0; i < varList.Nodes.Count; i++) {
                            AddPair((IdentifierExpression) varList.Nodes[i], nodes);
                        }

                        return;
                    }
                }
            }

            if (varNodes is IdentifierExpression varNode && exprNodes is Expression exprNode) {
                AddPair(varNode, exprNode);
                return;
            }

            DiagnosticManager.ThrowFatal(
                new ArgumentException($"Invalid arguments ({varNodes.GetType().ToFullLinkedName()}, {exprNodes?.GetType()?.ToFullLinkedName()}) for VariableDeclarationNode.AddPairs"), this);
            // throw new ArgumentException($"Invalid arguments ({varNodes.GetType().ToFullLinkedName()}, {exprNodes.GetType().ToFullLinkedName()}) for VariableDeclarationNode.AddPairs");
        }

        public void AddPair(IdentifierExpression varNode, Expression exprNode) {
            Initializers.Add(new VariableInitializerNode(varNode, exprNode));
        }

        public override Maybe<ValueReference> Execute(ExecContext ctx) {
            return Initializers[0].Execute(ctx).ToMaybe();
        }
        public override IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
            foreach (var initializer in Initializers) {
                yield return initializer.Execute(ctx).ToMaybe();
            }
        }
        public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
            foreach (var i in Initializers) {
                foreach (var t in i.ResolveAndCacheTypes(ctx, symbol)) {
                    yield return t;
                }
            }
        }

        public override IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
            var s = scope ?? Scope;
            return Initializers.SelectMany(x => x.FindReferences(s));
        }
        public IEnumerable<NamedSymbolInformation> GetNamedSymbols() {
            return Initializers
               .Select(x => x.GetNamedSymbols())
               .SelectMany(x => x);
        }
    }

    [ASTNode]
    public partial class ArgumentDeclarationNode : BaseNode
    {
        [VisitableNodeProperty]
        public IdentifierExpression Name { get; set; }

        [VisitableNodeProperty]
        public TypeIdentifierExpression TypeIdentifier { get; set; }

        public bool IsVariadic  { get; set; }
        public Type VarArgsType => IsVariadic ? typeof(object[]) : null;
        public Type NativeType  { get; set; }
        public int  Index       { get; set; }
        public bool IsNative    { get; set; }
        public bool IsOptional  { get; set; }

        public ArgumentDeclarationNode() { }

        public ArgumentDeclarationNode SetName(IdentifierExpression ident, bool setEndToken = true) {
            Name = ident;
            if (setEndToken)
                EndToken = ident.EndToken;

            return this;
        }
        public ArgumentDeclarationNode SetType(TypeIdentifierExpression ident, bool setEndToken = true) {
            TypeIdentifier = ident;
            if (setEndToken)
                EndToken = ident.EndToken;

            return this;
        }
        
        public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
            return TypeIdentifier.ResolveAndCacheTypes(ctx, symbol);
        }
    }

    [ASTNode]
    public partial class ArgumentListDeclarationNode : NodeList<ArgumentDeclarationNode>
    {
        [VisitableNodeProperty]
        public List<ArgumentDeclarationNode> Arguments => Nodes;

        public bool HasVarArgs   { get; set; }
        public int  VarArgsIndex { get; set; }

        public ArgumentListDeclarationNode() { }
        public ArgumentListDeclarationNode(IEnumerable<ArgumentDeclarationNode> arguments) : base(arguments) { }


        public int GetValidArgumentCount(int callArgCount) {
            if (HasVarArgs) {
                return callArgCount;
            }

            return Math.Min(callArgCount, Arguments.Count);
        }

        public bool Get(int index, out ArgumentDeclarationNode node) {
            if (index < 0) {
                node = null;
                return false;
            }

            if (HasVarArgs && index >= VarArgsIndex) {
                node = Arguments[VarArgsIndex];
                return true;
            }

            if (index >= Arguments.Count) {
                if (HasVarArgs) {
                    node = Arguments[VarArgsIndex];
                    return true;
                }

                node = null;
                return false;
            }

            node = Arguments[index];
            return true;
        }

        public override void OnNodeAdded(ArgumentDeclarationNode node) {
            base.OnNodeAdded(node);

            if (node.IsVariadic && !HasVarArgs) {
                HasVarArgs   = true;
                VarArgsIndex = node.IsNative ? node.Index - 1 : node.Index;
            }

        }

    }

    [ASTNode]
    public partial class TupleListDeclarationNode : NodeList<BaseNode>
    {
        public TupleListDeclarationNode() { }
        public TupleListDeclarationNode(IEnumerable<BaseNode> arguments) : base(arguments) { }
    }

    [ASTNode]
    public partial class DefDeclaration_FunctionNode : BaseNode, ITopLevelDeclarationNode
    {
        [VisitableNodeProperty]
        public ArgumentListDeclarationNode Parameters { get; set; } = new();

        public DeclarationContext DeclarationContext { get; set; } = new();

        public string Name { get; set; }

        public bool IsStatic    { get; set; }
        public bool IsNative    { get; set; }
        public bool IsAsync     { get; set; }
        public bool IsCoroutine { get; set; }

        private Action<FunctionExecContext> _nativeFunction;
        public Action<FunctionExecContext> NativeFunction {
            get => _nativeFunction;
            set {
                _nativeFunction = value;
                IsNative        = value != null;
            }
        }

        public DefDeclaration_FunctionNode(string name) {
            Name = name;
        }

    }

}