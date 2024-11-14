using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.Interpreter.SyntaxAnalysis;

public class TypeAnalyzer
{
    public static TypeAnalyzer Instance = new();

    private static bool _initialized = false;

    public ProgramExpression Program { get; set; }
    public ExecContext Ctx     { get; set; }

    public Dictionary<BaseNode, ITypeAlias> EvaluatedTypes { get; } = new();

    public TypeAnalyzer() {
        if (_initialized) 
            return;
        
        _initialized = true;
        SymbolTable.Initialize();
    }

    public TypeAnalyzer(ProgramExpression program, ExecContext ctx) {
        Program = program;
        Ctx     = new ExecContext(ctx);
    }

    public static TypeAnalyzer TypeCheck(ProgramExpression program, ExecContext ctx) {
        var analyzer = new TypeAnalyzer(program, ctx);

        foreach (var node in program.Nodes) {
            analyzer.ResolveType(node);
        }

        return analyzer;
    }

    private ITypeAlias StoreEvaluatedType(BaseNode node, ITypeAlias evaluatedType) {
        EvaluatedTypes[node] = evaluatedType;
        // node.Type            = evaluatedType;

        return evaluatedType;
    }

    /*public static ITypeAlias ResolveTypeReference(TypeReference reference) {
        if (reference?.FromNode == null) {
            Instance.LogError(null, "Type reference is null");
            return null;
        }

        if (reference?.IsResolvedOrDefined() == true) {
            return reference.Get();
        }

        if (reference.Name != null) {
            if (TypeTable.TryGet(reference.Name, out var type)) {
                reference.Set(type, false);
                return type;
            }
        }

        if (Instance.EvaluatedTypes.TryGetValue(reference.FromNode, out var expression)) {
            return expression;
        }

        return Instance.ResolveType(reference.FromNode);
    }
    public static void RegisterTypeReference(TypeReference typeReference) {
        if (typeReference.FromNode == null) {
            Instance.LogError(null, "Type reference is null");
            return;
        }

        Instance.EvaluatedTypes[typeReference.FromNode] = typeReference.Type;
    }
    */

    public ITypeAlias ResolveType(BaseNode node) {
        if (EvaluatedTypes.TryGetValue(node, out var type)) {
            return type;
        }

        ITypeAlias HandleResult(ITypeAlias result) {
            if (result == null) {
                LogError(node, $"Failed to resolve type for {node.GetType().Name}");
            }

            return StoreEvaluatedType(node, result);
        }

        switch (node) {
            case IDeclarationNode declaration:
                return HandleResult(ProcessDeclaration(declaration));

            case Expression expr:
                return HandleResult(ResolveExpressionType(expr));

            case ReturnStatement statement:
                return HandleResult(ResolveType(statement.ReturnValue));

            default: {
                LogError(node, $"Type checking for {node.GetType().Name} is not implemented");

                return null;
            }
        }
    }

    public ITypeAlias ResolveBinaryExpression(Expression leftNode, OperatorType op, Expression rightNode) {
        var left = ResolveExpressionType(leftNode);
        if (left == null && leftNode != null) {
            LogError(leftNode, "Failed to resolve type for left side of binary expression");
            return null;
        }

        var right = ResolveExpressionType(rightNode);
        if (right == null && rightNode != null) {
            LogError(rightNode, "Failed to resolve type for right side of binary expression");
            return null;
        }

        return null;
        /*
        var precedenceType = TypeCoercionRegistry.GetHigherPrecedence(left, right);
        if (precedenceType == null) {
            LogError(leftNode, $"Cannot perform operation {op} on {left!.Name} and {right!.Name}");
            return null;
        }

        return precedenceType;*/
    }


    public ITypeAlias ResolveExpressionType(Expression expr) {
        switch (expr) {

            case LiteralValueExpression literal:
                return ResolveLiteralType(literal);

            case IdentifierExpression variable:
                return ResolveVariable(variable);

            case CallExpression call:
                return ResolveFunctionCallType(call);

            case BinaryOpExpression binary:
                return ResolveBinaryExpression(binary.Left as Expression, binary.Operator, binary.Right as Expression);

            default: {
                LogErrorWithStack(expr, $"Type checking for {expr.GetType().Name} is not implemented");
                return null;
            }
        }
    }

    public ITypeAlias ResolveLiteralType(LiteralValueExpression node) {
        try {
            return node.GetTypeAlias();
        }
        catch (FailedToGetRuntimeTypeException e) {
            LogError(node, e.Message);
            return null;
        }
    }

    private ITypeAlias ResolveVariable(IdentifierExpression variable)
        => SymbolTable.GetVariableType(variable.Name, variable);

    private ITypeAlias ResolveFunctionCallType(CallExpression call) {
        var returnType = SymbolTable.GetFunctionReturnType(call.Name, call);
        if (returnType != null)
            return returnType;

        if (Ctx.Functions.Get(call.Name, out var function)) {
            if (function.IsNative) {
                return TypeAlias<UnitPrototype>.Get();
            }

            return function.ReturnType.ResolveType();
        }

        LogError(call, $"Function {call.Name} not found");
        return null;
    }

    private ITypeAlias ProcessDeclaration(IDeclarationNode declarationInterface) {
        var declaration = (declarationInterface as BaseNode)!;

        switch (declaration) {
            case VariableDeclarationNode variable:
                return ProcessVariableDeclaration(variable);

            case FunctionDeclaration function:
                return ProcessFunctionDeclaration(function);

            // case SignalDeclaration signal:
            // return ProcessSignalDeclaration(signal);

            default:
                LogError(declaration, $"Declaration type {declaration.GetType().Name} not implemented");
                return null;
        }
    }

    private ITypeAlias ProcessVariableDeclaration(VariableDeclarationNode node) {
        foreach (var initializer in node.Initializers) {
            switch (initializer.Val) {
                case LiteralValueExpression literal: {
                    var t = ResolveType(literal);
                    SymbolTable.DefineVariable(initializer.Name, t);
                    return StoreEvaluatedType(initializer, t);
                }

                case CallExpression call: {
                    var returnType = ResolveType(call);
                    SymbolTable.DefineVariable(initializer.Name, returnType);
                    return StoreEvaluatedType(initializer, returnType);
                }

                default: {
                    LogError(initializer, $"Cannot resolve type for initializer {initializer.Val.GetType().Name}");
                    return null;
                }
            }
        }

        LogError(node, "Failed to resolve type for variable declaration");

        return null;
    }

    private ITypeAlias ProcessFunctionDeclaration(FunctionDeclaration node) {
        using var _ = SymbolTable.UsingScope();

        var returnType = node.ReturnType.ResolveType();
        SymbolTable.DefineFunction(node.Name, returnType);

        var (returnNode, returnStatementType) = ProcessBlock(node.Body, false);
        if (returnStatementType != returnType) {
            // if (returnType == null) {
            //     node.ReturnType.SetType(returnStatementType, null, false);
            // }

            if (returnStatementType != node.ReturnType.ResolveType()) {
                LogError(returnNode, $"Expected return type {returnType.Name}, got {returnStatementType.Name}");
            }
        }

        return returnType;
    }

    private ITypeAlias ProcessSignalDeclaration(SignalDeclaration node) {
        return null;
    }
    private (ReturnStatement returnNode, ITypeAlias returnStatementType) ProcessBlock(BlockExpression node, bool pushScope = true) {
        if (pushScope) {
            SymbolTable.PushScope();
        }

        ReturnStatement returnNode          = null;
        ITypeAlias         returnStatementType = null;

        foreach (var statement in node) {
            var t = ResolveType(statement);

            if (statement is ReturnStatement n) {
                returnNode          = n;
                returnStatementType = t;
            }
        }

        if (pushScope) {
            SymbolTable.PopScope();
        }

        return (returnNode, returnStatementType);
    }

    /*public ITypeAlias EvaluateExpression(BaseNode node) {
        if (EvaluatedTypes.TryGetValue(node, out var expression)) {
            return expression;
        }

        if (node is not IExpression expr) {
            throw new Exception($"Expected expression node, got {node.GetType().Name}");
        }

        ITypeAlias evaluatedType = null;

        switch (node) {
            case LiteralValueNode literal: {
                evaluatedType = literal.GetITypeAlias();
                break;
            }
            case VariableNode variable: {
                if (Ctx.Variables.Get(variable.Name, out var symbol)) {
                    evaluatedType = symbol.Type;
                }

                break;
            }
            case MemberAccessExpression variable: {
                if (variable.Object != null) {
                    var objType = EvaluateExpression(variable.Object);

                    if (objType is ITypeAliasInfo_Object obj) {
                        if (obj.Fields.TryGetValue(variable.Name, out var field)) {
                            return StoreEvaluatedType(node, field.ValueConstructor().ITypeAlias);
                        }

                        LogError(node, $"Field {variable.Name} not found in object {obj.Name}");
                    }

                    LogError(node, $"Expected object type, got {objType.Name}");
                }

                break;
            }
            case VariableDeclarationNode variable: {
                break;
            }

            default: {
                LogError(node, $"Type checking for {node.GetType().Name} is not implemented");
                break;
            }
        }

        if (evaluatedType != null) {
            return StoreEvaluatedType(node, evaluatedType);
        }

        return null;
    }*/

    private void LogError(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Create(node, file, line, member).LogFatal(message, node);
    }
    private void LogErrorWithStack(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Create(node, file, line, member).LogFatal(message, node, true);
    }
}