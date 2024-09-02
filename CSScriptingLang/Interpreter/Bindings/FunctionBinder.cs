using System.Reflection;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;
using CSScriptingLang.Utils.ReflectionUtils;
using Engine.Engine.Logging;

namespace CSScriptingLang.Interpreter.Bindings;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class NativeFunctionBindAttribute : Attribute
{
    public string Name { get; set; }
    public NativeFunctionBindAttribute(string name) {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = true)]
public class NativeFunctionParameterBindAttribute : Attribute
{
    public RTVT Type { get; set; }
    public NativeFunctionParameterBindAttribute(RTVT type) {
        Type = type;
    }
}

public struct NativeFunctionExecutionContext
{
    public Interpreter            Interpreter { get; set; }
    public FunctionExecutionFrame Frame       { get; set; }
}

public class FunctionBinder
{
    private static Logger Logger = Logs.Get<FunctionBinder>();

    public static Dictionary<MethodInfo, NativeBoundFunctionDeclarationNode> NativeFunctionBindingDeclarations = new();

    static FunctionBinder() {
        ClassScopedTimer<FunctionBinder>.SetColorFn(n => n.BoldGreen());
        ClassScopedTimer<FunctionBinder>.SetName("NativeFunctionBinder");
    }

    public static IEnumerable<MethodInfo> GetNativeFunctionBindings() {
        using var _ = ClassScopedTimer<FunctionBinder>.New();

        foreach (var methodInfo in ReflectionStore.AllMethodsWithAttribute<NativeFunctionBindAttribute>(BindingFlags.Static | BindingFlags.Public)) {
            yield return methodInfo;
        }
    }

    public static void BindNativeFunctions(Interpreter interpreter) {
        var methods = GetNativeFunctionBindings().ToList();

        using var _ = ClassScopedTimer<FunctionBinder>.NewPrefixed($"Binding {methods.Count} native functions");

        foreach (var methodInfo in methods) {
            BindNativeFunction(methodInfo);
        }

        foreach (var pair in NativeFunctionBindingDeclarations) {
            interpreter.Symbols.DeclareFunction(pair.Value);
        }
    }

    public static NativeBoundFunctionDeclarationNode BindNativeFunction(MethodInfo methodInfo) {
        var attribute = methodInfo.GetCustomAttribute<NativeFunctionBindAttribute>();
        if (attribute == null) {
            throw new Exception($"Method({methodInfo.Name}) is missing NativeFunctionBindAttribute");
        }

        var       bindingName = attribute.Name ?? methodInfo.Name;
        using var _           = ClassScopedTimer<FunctionBinder>.NewPrefixed($"{bindingName}");

        var declaration = new NativeBoundFunctionDeclarationNode(bindingName, methodInfo);
        var parameters  = methodInfo.GetParameters();

        // First parameter is always the NativeFunctionExecutionContext
        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(NativeFunctionExecutionContext)) {
            throw new Exception($"Method({bindingName}) must have a NativeFunctionExecutionContext as the first parameter");
        }

        for (var i = 1; i < parameters.Length; i++) {
            var param = parameters[i];

            if (param.ParameterType == typeof(RuntimeValue)) {
                var typeName           = "";
                var parameterAttribute = param.GetCustomAttribute<NativeFunctionParameterBindAttribute>();
                if (parameterAttribute != null) {
                    typeName = parameterAttribute.Type.Name();
                }

                var argDecl = new ArgumentDeclarationNode(param.Name, typeName) {
                    NativeType = param.ParameterType,
                    Index      = i,
                    IsNative   = true
                };

                declaration.Parameters.Add(argDecl);

                continue;
            }

            // Parameter type can either be RuntimeValue or `params object[]`
            if (param.ParameterType == typeof(object[])) {
                // check if `params`
                if (i != parameters.Length - 1) {
                    throw new Exception($"Method({bindingName}) `params object[]` must be the last parameter");
                }

                var argDecl = ArgumentDeclarationNode.VarArgs(param.Name);
                argDecl.Index      = i;
                argDecl.NativeType = param.ParameterType;
                argDecl.IsNative   = true;

                declaration.Parameters.Add(argDecl);
                continue;
            }


            throw new Exception($"Method({bindingName}) parameter({param.Name}) must be either `RuntimeValue` or `params object[]`");

        }

        declaration.NativeFunction = (interpreter, frame) => {
            NativeFunctionExecutionContext context = new() {
                Interpreter = interpreter,
                Frame       = frame
            };

            var args = new List<object> {context};
            // Set args size to `declaration.Parameters.Count` to avoid resizing
            args.Capacity = declaration.Parameters.Count + 1;

            var frameArgs  = frame.Args.ToList();
            var varArgsArr = new List<RuntimeValue>();

            var idx = 0;
            foreach (var arg in declaration.Parameters) {
                if (arg.IsVarArgs) {
                    for (var i = idx; i < frameArgs.Count; i++) {
                        if (frameArgs[i].Value is RuntimeValue_Array rtc) {
                            varArgsArr.AddRange((rtc.Value as List<RuntimeValue>)!);
                        } else {
                            varArgsArr.Add(frameArgs[i].Value);
                        }
                    }

                } else {
                    var argValue = frame.Args[idx];
                    if (argValue == null) {
                        throw new Exception($"Method({bindingName}) missing parameter({arg.Name})");
                    }

                    args.Add(argValue.Value);
                }

                idx++;
            }

            if (varArgsArr.Count > 0)
                args.Add(varArgsArr.ToArray());

            var result = methodInfo.Invoke(null, args.ToArray());

            if (result is not null) {
                frame.ReturnValue = result switch {
                    RuntimeValue rtv => rtv,
                    _                => RuntimeValue.Rent(result)
                };
            }
        };

        NativeFunctionBindingDeclarations[methodInfo] = declaration;

        return declaration;
    }
}