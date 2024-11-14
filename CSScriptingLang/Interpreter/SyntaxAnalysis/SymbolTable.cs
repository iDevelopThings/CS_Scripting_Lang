using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Lexing;
using CSScriptingLang.Mixins;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter.SyntaxAnalysis;


[AddMixin(typeof(DiagnosticLoggingMixin))]
public static partial class SymbolTable
{
    private static Stack<Dictionary<string, ITypeAlias>> VariableTypesStack       = new();
    private static Stack<Dictionary<string, ITypeAlias>> FunctionReturnTypesStack = new();

    private static Dictionary<string, ITypeAlias> Variables           => VariableTypesStack.Peek();
    private static Dictionary<string, ITypeAlias> functionReturnTypes => FunctionReturnTypesStack.Peek();

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

    public static ITypeAlias GetVariableType(string name, BaseNode contextNode) {
        foreach (var scope in VariableTypesStack) {
            if (scope.TryGetValue(name, out var type)) {
                return type;
            }
        }
        
        Diagnostic_Error_Fatal().Message($"Variable '{name}' not defined.").Range(contextNode).Report();
        return null;
    }

    public static void DefineVariable(string name, ITypeAlias type) {
        Variables[name] = type;
    }

    public static ITypeAlias GetFunctionReturnType(string functionName, BaseNode contextNode) {
        foreach (var scope in FunctionReturnTypesStack) {
            if (scope.TryGetValue(functionName, out var type)) {
                return type;
            }
        }
        return null;
    }

    public static void DefineFunction(string functionName, ITypeAlias returnType) {
        functionReturnTypes[functionName] = returnType;
    }
}