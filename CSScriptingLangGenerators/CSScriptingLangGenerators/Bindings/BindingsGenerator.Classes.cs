using System.Linq;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public partial class BindingsGenerator
{
    private static void ClassBindings(
        GeneratorExecutionContext context,
        INamedTypeSymbol          klass,
        ClassTypeData             classData
    ) {
        // var classData = ClassTypeData.ForClassBinding(klass, context, outerWriter);
        var className = classData.Name;

        var w = classData;

        // var qualifier = classData.Qualifier;
        // var constructors  = GetConstructors(context, klass);
        // var properties    = GetProperties(context, klass, false).ToList();
        // var methods       = GetMethods(context, klass, false);
        // var staticMethods = GetMethods(context, klass, true);
        // methods = methods.Concat(staticMethods).ToList();
        // methods = methods.Concat(constructors.Select(c => (c, "#ctor", "__ctor"))).ToList();
        // var methodTables = MethodData.Build(context, methods);

        using (w.B("public sealed partial class Library : ILibrary")) {

            using (w.B("public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx)")) {
                using var _ = w.YieldBlock();

                w._("var prototype = Value.Object(ctx);");

                w._();

                classData.WritePropertyBindings("prototype");
                classData.WriteMethodBindings("prototype");

                w._();

                w._($"TypesTable.DeclareCustomObjectPrototype(ctx, \"{className}\", \"{klass.GetFullyQualifiedName()}\", prototype);");
                w._();

                classData.WriteConstructorDefinition();

            }

            w._();

            classData.WritePropertyDefinitions();

            w._();

            classData.WriteMethodDefinitions(true, true);

            /*foreach (var property in properties) {
                if (property.GetMethod != null) {
                    using (w.B($"public static Value {property.Name}__Getter(FunctionExecContext ctx, Value instance, params Value[] args)")) {
                        Prologue($"get{property.Name}");

                        using (w.B("if (args.Length != 0)")) {
                            w._($"throw new InterpreterRuntimeException(\"{className}.get{property.Name}: expected 0 arguments\");");
                        }

                        w._($"var value = obj.{property.Name};");

                        w._($"return {ConvertToValue(context, "value", property.Type, property.Property)};");
                    }
                }

                if (property.SetMethod != null) {
                    var parameter = Parameter.Create(context, property.SetMethod.Parameters[0]);

                    using (w.B($"public static Value {property.Name}__Setter(FunctionExecContext ctx, Value instance, params Value[] args)")) {

                        Prologue($"set{property.Name}");

                        using (w.B($"if (args.Length != 1 || !{CompareArgument(0, parameter)})")) {
                            w._($"throw new InterpreterRuntimeException(\"{className}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                        }

                        w._($"obj.{property.Name} = {ConvertFromValue(context, 0, property.Type, property.Property)};");

                        w._("return Value.Null();");
                    }

                    w._();
                }
            }*/

            /*foreach (var table in methodTables) {
                var isNormalMethod = table.Name != "#ctor";

                w._(isNormalMethod
                             ? $"public static Value {table.Identifier}__Dispatch(FunctionExecContext ctx, Value instance, params Value[] args)"
                             : $"public static Value {table.Identifier}__Dispatch(FunctionExecContext ctx, params Value[] args)");
                w.OpenBracket();

                if (isNormalMethod) {
                    Prologue(table.Name);
                }

                w._("switch (args.Length)");
                w.OpenBracket();

                for (var i = 0; i < table.Methods.Count; i++) {
                    var tableMethods = table.Methods[i];
                    if (tableMethods.Count == 0) {
                        continue;
                    }

                    using (w.B($"case {i}:")) {
                        var hasTrueType = false;
                        foreach (var method in tableMethods) {
                            var stmt = CompareArguments(method, out var isTrueType, i);
                            if (isTrueType)
                                hasTrueType = true;

                            using (w.B($"if ({stmt})")) {
                                CallMethod(context, writer, "obj", method, i);
                            }
                        }

                        if (!hasTrueType)
                            w._("break;");
                    }
                }

                w.CloseBracket();

                foreach (var method in table.ParamsMethods) {
                    using (w.B($"if (args.Length >= {method.RequiredParameterCount} && {CompareArguments(method)})")) {
                        CallMethod(context, writer, "obj", method);
                    }
                }

                w._();
                var errorPrefix  = $"{className}.{table.Name}: ";
                var errorMessage = GetMethodNotMatchedErrorMessage(errorPrefix, table);
                w._($"throw new InterpreterRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

                w.CloseBracket();
                w._();
            }*/

        }


        // outerWriter.Write(w);

    }
}