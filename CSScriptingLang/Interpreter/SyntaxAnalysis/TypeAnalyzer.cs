using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter.SyntaxAnalysis;

public class TypeAnalyzer
{
    public static TypeAnalyzer Instance = new();

    private static bool _initialized = false;

    public ProgramExpression Program { get; set; }
    public ExecContext Ctx     { get; set; }

    public Dictionary<BaseNode, RuntimeType> EvaluatedTypes { get; } = new();

    public TypeAnalyzer() {
        if (!_initialized) {
            _initialized = true;
            SymbolTable.Initialize();
        }
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

    private RuntimeType StoreEvaluatedType(BaseNode node, RuntimeType evaluatedType) {
        EvaluatedTypes[node] = evaluatedType;
        node.Type            = evaluatedType;

        return evaluatedType;
    }

    public static RuntimeType ResolveTypeReference(TypeReference reference) {
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

    public RuntimeType ResolveType(BaseNode node) {
        if (EvaluatedTypes.TryGetValue(node, out var type)) {
            return type;
        }

        RuntimeType HandleResult(RuntimeType result) {
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

    public RuntimeType ResolveBinaryExpression(Expression leftNode, OperatorType op, Expression rightNode) {
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

        var precedenceType = TypeCoercionRegistry.GetHigherPrecedence(left, right);
        if (precedenceType == null) {
            LogError(leftNode, $"Cannot perform operation {op} on {left!.Name} and {right!.Name}");
            return null;
        }

        return precedenceType;
    }


    public RuntimeType ResolveExpressionType(Expression expr) {
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

    public RuntimeType ResolveLiteralType(LiteralValueExpression node) {
        try {
            return node.GetRuntimeType();
        }
        catch (FailedToGetRuntimeTypeException e) {
            LogError(node, e.Message);
            return null;
        }
    }

    private RuntimeType ResolveVariable(IdentifierExpression variable)
        => SymbolTable.GetVariableType(variable.Name, variable);

    private RuntimeType ResolveFunctionCallType(CallExpression call) {
        var returnType = SymbolTable.GetFunctionReturnType(call.Name, call);
        if (returnType != null)
            return returnType;

        if (Ctx.Functions.Get(call.Name, out var function)) {
            if (function.IsNative) {
                return StaticTypes.Null;
            }

            return function.ReturnType.Type;
        }

        LogError(call, $"Function {call.Name} not found");
        return null;
    }

    private RuntimeType ProcessDeclaration(IDeclarationNode declarationInterface) {
        var declaration = (declarationInterface as BaseNode)!;

        switch (declaration) {
            case VariableDeclarationNode variable:
                return ProcessVariableDeclaration(variable);

            case FunctionDeclaration function:
                return ProcessFunctionDeclaration(function);

            // case SignalDeclarationNode signal:
            // return ProcessSignalDeclaration(signal);

            default:
                LogError(declaration, $"Declaration type {declaration.GetType().Name} not implemented");
                return null;
        }
    }

    private RuntimeType ProcessVariableDeclaration(VariableDeclarationNode node) {
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

    private RuntimeType ProcessFunctionDeclaration(FunctionDeclaration node) {
        using var _ = SymbolTable.UsingScope();

        var returnType = node.ReturnType.Get();
        SymbolTable.DefineFunction(node.Name, returnType);

        var (returnNode, returnStatementType) = ProcessBlock(node.Body, false);
        if (returnStatementType != returnType) {
            if (returnType == null) {
                node.ReturnType.SetType(returnStatementType, null, false);
            }

            if (returnStatementType != node.ReturnType.Get()) {
                LogError(returnNode, $"Expected return type {returnType.Name}, got {returnStatementType.Name}");
            }
        }

        return returnType;
    }

    private RuntimeType ProcessSignalDeclaration(SignalDeclarationNode node) {
        return null;
    }
    private (ReturnStatement returnNode, RuntimeType returnStatementType) ProcessBlock(BlockExpression node, bool pushScope = true) {
        if (pushScope) {
            SymbolTable.PushScope();
        }

        ReturnStatement returnNode          = null;
        RuntimeType         returnStatementType = null;

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

    /*public RuntimeType EvaluateExpression(BaseNode node) {
        if (EvaluatedTypes.TryGetValue(node, out var expression)) {
            return expression;
        }

        if (node is not IExpression expr) {
            throw new Exception($"Expected expression node, got {node.GetType().Name}");
        }

        RuntimeType evaluatedType = null;

        switch (node) {
            case LiteralValueNode literal: {
                evaluatedType = literal.GetRuntimeType();
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

                    if (objType is RuntimeTypeInfo_Object obj) {
                        if (obj.Fields.TryGetValue(variable.Name, out var field)) {
                            return StoreEvaluatedType(node, field.ValueConstructor().RuntimeType);
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