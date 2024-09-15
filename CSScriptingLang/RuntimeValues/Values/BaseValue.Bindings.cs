using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;
using CSScriptingLang.Utils.ReflectionUtils;

namespace CSScriptingLang.RuntimeValues.Values;

public abstract partial class BaseValue
{
    public static Dictionary<Type, Dictionary<string, NativeBoundFunctionDeclarationNode>> NativeMethodBindings { get; } = new();
    public static Dictionary<Type, Dictionary<string, NativeFieldBind>>                    NativeFieldBindings  { get; } = new();

    
    public class NativeFieldBind
    {
        public string Name { get; set; }

        // Creates the new runtime value instance for this field binding
        public Func<BaseValue, BaseValue> FieldBindValueFactory { get; set; }
    }
    
    public static void LoadNativeBindings() {
        var derivedTypes = typeof(BaseValue).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(BaseValue)));

        foreach (var type in derivedTypes) {
            LoadNativeMethodBindings(type);
            LoadNativeFieldBindings(type);
        }
    }

    private void InitNativeBindings() {
        if (NativeMethodBindings.TryGetValue(GetType(), out var bindings)) {
            foreach (var (name, decl) in bindings) {
                var rtDecl = ValueFactory.Function.Make(decl);
                SetField(name, rtDecl);
            }
        }

        if (NativeFieldBindings.TryGetValue(GetType(), out var fields)) {
            foreach (var bind in fields) {
                SetField(bind.Value.Name, bind.Value.FieldBindValueFactory(this));
            }
        }
    }

    public static Dictionary<string, NativeBoundFunctionDeclarationNode> LoadNativeMethodBindings(Type type) {
        var methods = type.MethodsWithAttribute<NativeFunctionBindAttribute>().ToList();

        var binds = new Dictionary<string, NativeBoundFunctionDeclarationNode>();

        foreach (var (method, attr) in methods) {
            var declaration = NativeBinder.CreateNativeBindingDeclaration(method);

            binds.Add(declaration.Name, declaration);
        }

        return NativeMethodBindings[type] = binds;
    }
    public static Dictionary<string, NativeFieldBind> LoadNativeFieldBindings(Type type) {

        var binds = new Dictionary<string, NativeFieldBind>();

        foreach (var member in type.GetPropertyProxies()) {
            if (!member.GetAttribute<NativeFieldBindAttribute>(out var attr))
                continue;

            var name = attr.Name ?? member.Name.FirstCharToLower();

            var binding = new NativeFieldBind {
                Name = name,
                FieldBindValueFactory = valueInstance => {
                    var rtValue = ValueFactory.Make(member.Type);
                    // var rtValue = member.Type.RuntimeType().ValueConstructor();

                    if (member.CanRead() && attr.CanRead) {
                        rtValue.GetterProxy = instance => {
                            return member.GetValue(valueInstance);
                        };
                    }

                    if (member.CanWrite() && attr.CanWrite) {
                        rtValue.SetterProxy = (instance, value) => {
                            member.SetValue(valueInstance, value);
                        };
                    }

                    return rtValue;
                }
            };

            binds[name] = binding;

        }

        return NativeFieldBindings[type] = binds;
    }

    public List<NativeBoundFunctionDeclarationNode> LoadNativeMethodBindings() {
        var type = GetType();

        if (NativeMethodBindings.TryGetValue(type, out var methodBindings)) {
            return methodBindings.Values.ToList();
        }

        return LoadNativeMethodBindings(type).Values.ToList();
    }

    public Dictionary<string, NativeFieldBind> LoadNativeFieldBindings() {
        var type = GetType();

        if (NativeFieldBindings.TryGetValue(type, out var fieldBindings)) {
            return fieldBindings;
        }

        return LoadNativeFieldBindings(type);
    }
}