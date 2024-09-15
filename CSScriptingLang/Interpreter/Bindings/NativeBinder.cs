using System.Reflection;
using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using CSScriptingLang.Utils.ReflectionUtils;
using Engine.Engine.Logging;

namespace CSScriptingLang.Interpreter.Bindings;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public class NativeFieldBindAttribute : Attribute
{
    public string Name     { get; set; }
    public bool   CanRead  { get; set; } = true;
    public bool   CanWrite { get; set; } = true;

    public NativeFieldBindAttribute() { }
    public NativeFieldBindAttribute(string name) {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class NativeFunctionBindAttribute : Attribute
{
    public string Name { get; set; }
    public NativeFunctionBindAttribute() { }
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
    public FunctionExecContext Ctx         { get; set; }
    public Interpreter         Interpreter => Ctx.Interpreter;

    public NativeBoundFunctionDeclarationNode Declaration { get; set; }

    public List<BaseValue> ReturnValues = new();

    public NativeFunctionExecutionContext() {
        Ctx         = null;
        Declaration = null;
    }
}

public class NativeBinder
{
    private static Logger Logger = Logs.Get<NativeBinder>(LogLevel.Warning);

    public static ClassScopedTimerInst<NativeBinder> Timer = ClassScopedTimerInst<NativeBinder>.Create(Logger)
       .SetColorFn(n => n.BoldGreen())
       .SetName("NativeBinder");

    public static Dictionary<MethodInfo, NativeBoundFunctionDeclarationNode> NativeFunctionBindingDeclarations = new();

    public static IEnumerable<MethodInfo> GetNativeFunctionBindings() {
        using var _ = Timer.New();

        foreach (var methodInfo in ReflectionStore.AllMethodsWithAttribute<NativeFunctionBindAttribute>(BindingFlags.Static | BindingFlags.Public)) {
            yield return methodInfo;
        }
    }


    public static Dictionary<MethodInfo, NativeBoundFunctionDeclarationNode> BindNativeFunctions(ExecContext ctx) {
        
        /*
        var methods = GetNativeFunctionBindings().ToList();

        using var _ = Timer.NewPrefixed($"Binding {methods.Count} native functions");

        foreach (var methodInfo in methods) {
            if (!methodInfo.IsStatic) {
                continue;
            }

            BindNativeFunction(methodInfo);
        }

        foreach (var pair in NativeFunctionBindingDeclarations) {
            ctx.DeclareFunction(pair.Value);
        }
        
        BaseValue.LoadNativeBindings();
        foreach (var pair in BaseValue.NativeMethodBindings) {
            foreach (var fn in pair.Value.Values) {
                var owner = fn.IsStatic ? null : TypeTable.Current.FromValueType(pair.Key);
                ctx.DeclareFunction(fn, owner);
            }
        }
        */

        return NativeFunctionBindingDeclarations;
    }

    public static NativeBoundFunctionDeclarationNode CreateNativeBindingDeclaration(MethodInfo methodInfo) {
        var attribute = methodInfo.GetCustomAttribute<NativeFunctionBindAttribute>();
        if (attribute == null) {
            throw new Exception($"Method({methodInfo.Name}) is missing NativeFunctionBindAttribute");
        }

        var bindingName = attribute.Name ?? methodInfo.Name.FirstCharToLower();

        if (ModuleResolver.Declarations.DefFunctionsDeclarations.TryGetValue(bindingName, out var decl)) {
            /*var thisArg = decl.Parameters.FirstOrDefault(p => p.Name == "this");
            var args = (thisArg != null ? decl.Parameters.Skip(1) : decl.Parameters)
               .Select(p => new ArgumentDeclarationNode(p.Name, p.TypeName) {
                        Type = TypeTable.TryGet(p.TypeName)
                    }
                )
               .ToList();
            Console.WriteLine();*/
        }

        using var _ = Timer.NewPrefixed($"{methodInfo.ToClassNameSpaceMethodName(false)} -> {bindingName}");

        var declaration = new NativeBoundFunctionDeclarationNode(bindingName, methodInfo) {
            IsAsync  = methodInfo.IsAsync(),
            IsStatic = methodInfo.IsStatic
        };

        var parameters = methodInfo.GetParameters();

        // First parameter is always the NativeFunctionExecutionContext
        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(NativeFunctionExecutionContext).MakeByRefType()) {
            // var nsClassMethod = methodInfo.DeclaringType?.Name + "." + methodInfo.Name;
            var err =
                $"Method({methodInfo.ToClassNameSpaceMethodName()}) must have a `ref NativeFunctionExecutionContext` as the first parameter; Example: `public void {methodInfo.Name}(ref NativeFunctionExecutionContext ctx)`";
            Logger.Error(err);

            throw new Exception(err);
        }

        BindMethodParameters(parameters, declaration);
        BindReturnType(methodInfo, declaration);

        declaration.NativeFunction = (ctx) => {
            NativeFunctionExecutionContext context = new() {
                Ctx         = ctx,
                Declaration = declaration
            };

            var target = methodInfo.IsStatic ? null : ctx.This;

            var args   = ConstructExecArgs(ref context);
            var result = methodInfo.Invoke(target, args.ToArray());

            if (result is not null) {
                foreach (var rtv in GetResultValues(result)) {
                    ctx.ReturnValues.Add(rtv);
                }
            }
        };

        return declaration;
    }

    public static void BindReturnType(MethodInfo method, NativeBoundFunctionDeclarationNode declaration) {
        var rt = method.ReturnType;
        if (rt == typeof(void))
            return;

        void AddReturnType(int argIdx, Type type) {
            if (type.IsAssignableTo(typeof(BaseValue))) {
                declaration.NativeReturnTypes.Add(type);
                return;
            }

            throw new Exception(
                $"Method({method.ToClassNameSpaceMethodName()}) must return (`void`, `BaseValue` or `ExecResult`) or... a tuple of `BaseValues`. Arg: {argIdx} is the wrong type({type.Name})");

        }

        if (method.IsAsync()) {
            declaration.IsAsync = true;

            if (rt.IsGenericType && rt.GetGenericTypeDefinition().IsAssignableTo(typeof(Task<>))) {
                var taskType = rt.GetGenericArguments()[0];
                AddReturnType(0, taskType);
                return;
            }

            AddReturnType(0, rt);
            return;
        }

        // check if we return `(x, y, z)` tuple
        if (rt.IsGenericType && rt.GetGenericTypeDefinition().IsAssignableTo(typeof(ITuple))) {
            var tupleTypes = rt.GetGenericArguments();

            for (var i = 0; i < tupleTypes.Length; i++) {
                AddReturnType(i, tupleTypes[i]);
            }

            return;
        }


        AddReturnType(0, rt);
    }

    public static List<object> ConstructExecArgs(
        ref NativeFunctionExecutionContext fnCtx
    ) {
        var ctx = fnCtx.Ctx;

        var args     = new List<object> {fnCtx};
        var argCount = Math.Max(fnCtx.Declaration.Parameters.Count, ctx.Params.Count);
        for (var i = 0; i < argCount; i++) {
            var arg     = ctx.Params.ElementAtOrDefault(i);
            var argDecl = fnCtx.Declaration.Parameters.Arguments.ElementAtOrDefault(i);

            if (arg == null && argDecl == null) {
                throw new Exception($"Method({fnCtx.Declaration.Name}) missing parameter({i})");
            }

            /*if(argDecl?.Name == "this") {
                args.Add(ctx.This);
                continue;
            }*/
            if (arg == null) {
                if (argDecl.IsOptional) {
                    args.Add(null);
                    continue;
                }

                throw new Exception($"Method({fnCtx.Declaration.Name}) missing parameter({argDecl.Name})");
            }

            if (arg.Val.Is.Array && argDecl.IsVariadic) {
                args.Add(arg.Val.As.List().ToArray());
                continue;
            }

            args.Add(arg.Val);
        }

        return args;
    }

    public static void BindMethodParameters(ParameterInfo[] parameters, NativeBoundFunctionDeclarationNode declaration) {
        if (!declaration.IsStatic) {
            // param 0 should be NativeFunctionExecutionContext, param 1 should be `RuntimeValue_Object @this`
            if (parameters.Length < 2 || !parameters[1].ParameterType.IsAssignableTo(typeof(BaseValue))) {
                throw new Exception($"Method({declaration.MethodInfo.ToClassNameSpaceMethodName()}) must have a `BaseValue @this` as the second parameter if it's not static");
            }

            if (parameters[1].Name != "this") {
                throw new Exception($"Method({declaration.MethodInfo.ToClassNameSpaceMethodName()}) must have a `BaseValue_Object @this` as the second parameter if it's not static");
            }
        }

        for (var i = 1; i < parameters.Length; i++) {
            var param = parameters[i];

            if (param.ParameterType.IsAssignableTo(typeof(BaseValue))) {
                var typeName           = "";
                var parameterAttribute = param.GetCustomAttribute<NativeFunctionParameterBindAttribute>();
                if (parameterAttribute != null) {
                    typeName = parameterAttribute.Type.Name();
                }

                var argDecl = new ArgumentDeclarationNode(param.Name, typeName) {
                    NativeType = param.ParameterType,
                    Index      = i,
                    IsNative   = true,
                    IsOptional = param.IsOptional
                };

                declaration.Parameters.Add(argDecl);

                continue;
            }

            // Parameter type can either be BaseValue or `params object[]`
            if (param.ParameterType.IsAssignableTo(typeof(object[]))) {
                // check if `params`
                if (i != parameters.Length - 1) {
                    throw new Exception($"Method({declaration.MethodInfo.ToClassNameSpaceMethodName()}) `params object[]` must be the last parameter");
                }

                var argDecl = ArgumentDeclarationNode.VarArgs(param.Name);
                argDecl.Index      = i;
                argDecl.NativeType = param.ParameterType;
                argDecl.IsNative   = true;

                declaration.Parameters.Add(argDecl);
                continue;
            }


            throw new Exception($"Method({declaration.MethodInfo.ToClassNameSpaceMethodName()}) parameter({param.Name}) must be either `BaseValue` or `params object[]`");

        }
    }

    public static IEnumerable<Value> GetResultValues(object returnValue) {
        switch (returnValue) {
            case Value rtv:
                yield return rtv;
                break;

            case ITuple tuple:
                for (var i = 0; i < tuple.Length; i++) {
                    yield return (Value) tuple[i];
                }

                break;

            case IEnumerable<Value> rtvList:
                foreach (var rtv in rtvList) {
                    yield return rtv;
                }

                break;
        }
    }

}