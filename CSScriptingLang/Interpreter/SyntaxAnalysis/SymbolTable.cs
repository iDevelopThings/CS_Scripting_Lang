using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter.SyntaxAnalysis;

public static class SymbolTable
{
    private static Stack<Dictionary<string, RuntimeType>> VariableTypesStack       = new();
    private static Stack<Dictionary<string, RuntimeType>> FunctionReturnTypesStack = new();

    private static Dictionary<string, RuntimeType> Variables           => VariableTypesStack.Peek();
    private static Dictionary<string, RuntimeType> functionReturnTypes => FunctionReturnTypesStack.Peek();

    public static void Initialize() {
        VariableTypesStack.Clear();
        FunctionReturnTypesStack.Clear();
        
        PushScope();
    }

    public static UsingCallbackHandle UsingScope() {
        PushScope();
        return new UsingCallbackHandle(PopScope);
    }
    
    public static void PushScope() {
        VariableTypesStack.Push(new());
        FunctionReturnTypesStack.Push(new());
    }

    public static void PopScope() {
        VariableTypesStack.Pop();
        FunctionReturnTypesStack.Pop();
    }

    public static RuntimeType GetVariableType(string name, BaseNode contextNode) {
        foreach (var scope in VariableTypesStack) {
            if (scope.TryGetValue(name, out var type)) {
                return type;
            }
        }
        
        throw new DeclarationException($"Variable '{name}' not defined.", contextNode, contextNode?.GetScript());
    }

    public static void DefineVariable(string name, RuntimeType type) {
        Variables[name] = type;
    }

    public static RuntimeType GetFunctionReturnType(string functionName, BaseNode contextNode) {
        foreach (var scope in FunctionReturnTypesStack) {
            if (scope.TryGetValue(functionName, out var type)) {
                return type;
            }
        }
        return null;
    }

    public static void DefineFunction(string functionName, RuntimeType returnType) {
        functionReturnTypes[functionName] = returnType;
    }
}