using System.Linq;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public partial class BindingsGenerator
{
    private static void ClassBindings(GeneratorExecutionContext context, INamedTypeSymbol klass, Writer writer) {
        var className = klass.GetAttributeArgument<string>(Attributes.Class, klass.Name);


        var qualifier     = $"global::{klass.GetFullyQualifiedName()}";
        var constructors  = GetConstructors(context, klass);
        var properties    = GetProperties(context, klass, false).ToList();
        var methods       = GetMethods(context, klass, false);
        var staticMethods = GetMethods(context, klass, true);

        methods = methods.Concat(staticMethods).ToList();

        var methodTables = MethodData.Build(context, methods.Concat(constructors.Select(c => (c, "#ctor", "__ctor"))));

        using (writer.B("public sealed partial class Library : ILibrary")) {

            using (writer.B("public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx)")) {

                writer._("var prototype = Value.Object();");

                writer._();

                foreach (var property in properties) {
                    if (property.GetMethod != null) {
                        writer._($"prototype[\"get{property.Name}\"] = Value.Function(\"{property.Name}\", {property.Name}__Getter);");
                    }

                    if (property.SetMethod != null) {
                        writer._($"prototype[\"set{property.Name}\"] = Value.Function(\"{property.Name}\", {property.Name}__Setter);");
                    }
                }

                foreach (var table in methodTables) {
                    writer._($"prototype[\"{table.Identifier}\"] = Value.Function(\"{table.Identifier}\", {table.Identifier}__Dispatch);");
                }

                writer._();

                writer._($"ctx.DeclarePrototype(\"{className}\", \"{klass.GetFullyQualifiedName()}\", prototype);");
                writer._();

                if (constructors.Count > 0) {
                    writer._($"yield return new KeyValuePair<string, Value>(\"{className}\", Value.Function(\"{className}__ctor\", __ctor__Dispatch));");
                }

                writer._("yield break;");
            }

            writer._();

            foreach (var property in properties) {
                if (property.GetMethod != null) {
                    using (writer.B($"public static Value {property.Name}__Getter(FunctionExecContext ctx, Value instance, params Value[] args)")) {
                        Prologue($"get{property.Name}");

                        using(writer.B("if (args.Length != 0)")) {
                            writer._($"throw new InterpreterRuntimeException(\"{className}.get{property.Name}: expected 0 arguments\");");
                        }

                        writer._($"var value = obj.{property.Name};");
                        
                        writer._($"return {ConvertToValue(context, "value", property.Type, property.Property)};"); 
                    }
                }

                if (property.SetMethod != null) {
                    var parameter = Parameter.Create(context, property.SetMethod.Parameters[0]);

                    using (writer.B($"public static Value {property.Name}__Setter(FunctionExecContext ctx, Value instance, params Value[] args)")) {

                        Prologue($"set{property.Name}");

                        using (writer.B($"if (args.Length != 1 || !{CompareArgument(0, parameter)})")) {
                            writer._($"throw new InterpreterRuntimeException(\"{className}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                        }

                        writer._($"obj.{property.Name} = {ConvertFromValue(context, 0, property.Type, property.Property)};");

                        writer._("return Value.Null();");
                    }

                    writer._();
                }
            }

            foreach (var table in methodTables) {
                var isNormalMethod = table.Name != "#ctor";

                writer._(isNormalMethod
                             ? $"public static Value {table.Identifier}__Dispatch(FunctionExecContext ctx, Value instance, params Value[] args)"
                             : $"public static Value {table.Identifier}__Dispatch(FunctionExecContext ctx, params Value[] args)");
                writer.OpenBracket();

                if (isNormalMethod) {
                    Prologue(table.Name);
                }

                writer._("switch (args.Length)");
                writer.OpenBracket();

                for (var i = 0; i < table.Methods.Count; i++) {
                    var tableMethods = table.Methods[i];
                    if (tableMethods.Count == 0) {
                        continue;
                    }

                    using (writer.B($"case {i}:")) {
                        var hasTrueType = false;
                        foreach (var method in tableMethods) {
                            var stmt = CompareArguments(method, out var isTrueType, i);
                            if (isTrueType)
                                hasTrueType = true;

                            using (writer.B($"if ({stmt})")) {
                                CallMethod(context, writer, "obj", method, i);
                            }
                        }

                        if (!hasTrueType)
                            writer._("break;");
                    }
                }

                writer.CloseBracket();

                foreach (var method in table.ParamsMethods) {
                    using (writer.B($"if (args.Length >= {method.RequiredParameterCount} && {CompareArguments(method)})")) {
                        CallMethod(context, writer, "obj", method);
                    }
                }

                writer._();
                var errorPrefix  = $"{className}.{table.Name}: ";
                var errorMessage = GetMethodNotMatchedErrorMessage(errorPrefix, table);
                writer._($"throw new InterpreterRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

                writer.CloseBracket();
                writer._();
            }

        }

        return;

        void Prologue(string methodName) {
            using (writer.B("if (instance.Type != RTVT.Object)")) {
                writer._($"throw new InterpreterRuntimeException(\"{className}.{methodName}: can only be called on an instance of {className}\");");
            }

            if (klass.HasBaseType(TypeData.Value)) {
                using (writer.B($"if (instance is not {qualifier} obj)")) {
                    writer._($"throw new InterpreterRuntimeException(\"{className}.{methodName}: can only be called on an instance of {className}\");");
                }
            } else {
                using (writer.B($"if (instance?.GetUntypedValue() is not {qualifier} obj)")) {
                    writer._($"throw new InterpreterRuntimeException(\"{className}.{methodName}: can only be called on an instance of {className}\");");
                }
            }

            writer._();
        }
    }
}