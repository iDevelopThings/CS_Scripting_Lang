using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public partial class BindingsGenerator
{
    private static void PrototypeBindings(
        GeneratorExecutionContext context,
        INamedTypeSymbol          prototype,
        PrototypeTypeData         typeData
    ) {
        typeData.WriteDefinitions();

        using (typeData.B("public partial class PrototypeObject : IPrototypeObjectBinding")) {
            typeData.WriteBuildToMethod();
            typeData.WriteBuildMethod();
            typeData.WriteGetDefinitions();

            typeData.WritePropertyDefinitions();
            typeData.WriteMethodDefinitions(true, true);
        }

    }

    /*private static List<(IMethodSymbol Method, string Name, string Identifier)> GetInstanceGetters(GeneratorExecutionContext context, INamedTypeSymbol klass, bool? isStatic = null) {
        var result = new List<(IMethodSymbol, string, string)>();

        foreach (var member in klass.GetMembers()) {
            if (member is not IMethodSymbol {MethodKind: MethodKind.Ordinary} method || (isStatic != null && method.IsStatic != isStatic)) {
                continue;
            }

            var attributes        = method.GetAttributes();
            var hasInstanceGetter = attributes.TryGetAttribute(Attributes.InstanceGetterFunction, out var instanceGetterAttr);

            if (!hasInstanceGetter) {
                continue;
            }

            if (method.DeclaredAccessibility != Accessibility.Public) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, method.Locations.First()));
                continue;
            }

            var name = instanceGetterAttr.GetArgument<string>() ?? method.Name;
            result.Add((method, name, name));
        }

        return result;
    }
    private static void PrototypeBindingsOld(
        GeneratorExecutionContext context,
        INamedTypeSymbol          prototype,
        PrototypeTypeData         typeData
    ) {
        var writer = typeData;

        var prototypeName = prototype.GetAttributeArgument<string>(Attributes.Prototype, prototype.Name);
        var prototypeRtvt = prototype.GetAttributeArgument<int>(Attributes.Prototype, 0, 1);
        var properties    = GetProperties(context, prototype, true).ToList();
        var methods       = GetMethods(context, prototype, true);
        var constructors  = GetConstructors(context, prototype, true);
        var methodTables = MethodData.Build(
            context,
            methods.Concat(constructors.Select(c => (c, "#ctor", "__ctor")))
        );
        var instanceGetters = GetInstanceGetters(context, prototype, true);
        var instanceTables  = MethodData.Build(context, instanceGetters);


        writer._("public static PrototypeObject MainBuilder = new PrototypeObject();");
        writer._("public PrototypeObject Builder {get; set;} = MainBuilder;");

        // Only write if the prototype doesn't have a default constructor
        if (prototype.GetConstructors().Any(c => c.Parameters.Length == 0)) {
            writer._($"public {prototype.Name}() {{ throw new InterpreterRuntimeException($\"{{GetType().Name}} cannot be constructed without an ExecContext\"); }}");
        }

        using (writer.B("public partial class PrototypeObject : IPrototypeObjectBinding")) {
            using (writer.B($"public Value BuildTo(Value obj, Prototype protoDef, Value basePrototype = null)")) {

                writer._($"obj[\"symbolName\"] = Value.String(protoDef.Symbol.Name);");

                foreach (var property in properties) {
                    if (property.GetMethod != null) {
                        writer._($"obj[\"get{property.Name}\"] = Value.Function(\"{property.Name}\", {property.Name}__Getter);");
                    }

                    if (property.SetMethod != null) {
                        writer._($"obj[\"set{property.Name}\"] = Value.Function(\"{property.Name}\", {property.Name}__Setter);");
                    }
                }

                writer._();

                foreach (var table in instanceTables) {

                    var method = table.GetFirstMethod();

                    // writer._($"var value = {qualifier}.{property.Name};");
                    // writer._($"return {ConvertToValue(context, "value", property.Type, property.Property)};");
                    using (writer.B($"Value __get_{method.Identifier}(ExecContext ctx, Value instance)")) {
                        writer._(
                            $"var result = {method.Info.ContainingType.GetFullyQualifiedName()}.{method.Info.Name}(",
                            method.GetArgumentBindings(context, 100).Select(b => $"{writer.GetIndentString()}{b}").Join(", \n"),
                            ");",
                            $"return {ConvertToValue(context, "result", method.Info.ReturnType, method.Info)};"
                        );

                    }
                    writer._($"obj[\"{method.Identifier}\"] = Value.InstanceGetterFunction(\"{method.Identifier}\", __get_{method.Identifier});");
                }

                writer._();

                foreach (var table in methodTables) {
                    writer._($"obj[\"{table.Identifier}\"] = Value.Function(\"{table.Identifier}\", {table.Identifier}__Dispatch);");
                }

                writer._();

                using (writer.B("if (basePrototype != null)")) {
                    writer._("obj.Prototype = basePrototype;");
                }

                writer._();

                writer._("return obj;");
            }

            using (writer.B($"public Value Build(Prototype protoDef, ExecContext ctx, Value basePrototype = null)")) {
                writer._("var result = Value.Object(ctx);");
                writer._();
                writer._("result = BuildTo(result, protoDef, basePrototype);");
                writer._("result.Lock();");
                writer._("return result;");
            }

            using (writer.B("public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx)")) {

                foreach (var table in methodTables) {
                    if (!table.IsGlobal)
                        continue;

                    writer._("yield return new KeyValuePair<string, Value>");
                    using (writer.I("(", ");")) {
                        writer._($"\"{table.Name}\",");
                        writer._("Value.Function");
                        using (writer.I("(", ")")) {
                            writer._($"\"{table.Identifier}\",");
                            writer._($"{table.Identifier}__Dispatch");
                        }
                    }
                }

                if (constructors.Count > 0) {
                    writer._("yield return new KeyValuePair<string, Value>");
                    using (writer.I("(", ");")) {
                        writer._($"\"{prototypeName}\",");
                        writer._("Value.Function");
                        using (writer.I("(", ")")) {
                            writer._($"\"{prototypeName}__ctor\",");
                            writer._("__ctor__Dispatch");
                        }
                    }

                }

                writer._("yield break;");
            }


            var qualifier = $"global::{prototype.GetFullyQualifiedName()}";


            foreach (var property in properties) {
                if (property.GetMethod != null) {
                    using (writer.B($"private static Value {property.Name}__Getter(ExecContext ctx, Value instance, params Value[] args)")) {
                        using (writer.B("if (args.Length != 0)")) {
                            writer._($"throw new InterpreterRuntimeException(\"{prototypeName}.get{property.Name}: expected 0 arguments\");");
                        }

                        writer._($"var value = {qualifier}.{property.Name};");
                        writer._($"return {ConvertToValue(context, "value", property.Type, property.Property)};");
                    }
                    writer._();
                }

                if (property.SetMethod != null) {
                    var parameter = Parameter.Create(context, property.SetMethod.Parameters[0]);

                    using (writer.B($"private static Value {property.Name}__Setter(ExecContext ctx, Value instance, params Value[] args)")) {
                        using (writer.B($"if (args.Length != 1 || !{CompareArgument(0, parameter)})")) {
                            writer._($"throw new InterpreterRuntimeException(\"{prototypeName}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                        }
                        writer._($"{qualifier}.{property.Name} = {ConvertFromValue(context, 0, property.Type, property.Property)};");

                        writer._("return Value.Null();");
                    }
                    writer._();
                }
            }

            foreach (var table in methodTables) {
                var isNormalMethod = table.Name != "#ctor";
                using (writer.B($"public static Value {table.Identifier}__Dispatch(FunctionExecContext ctx, Value instance, params Value[] args)"
                           /*$"private static Value {table.Identifier}__Dispatch(FunctionExecContext ctx, Value instance, params Value[] args)"#1#
                       )) {

                    using (writer.B("switch (args.Length)")) {
                        for (var i = 0; i < table.Methods.Count; i++) {
                            var tableMethods = table.Methods[i];
                            if (tableMethods.Count == 0) {
                                continue;
                            }

                            using (writer.B($"case {i}:")) {
                                foreach (var method in tableMethods) {

                                    for (var paramIdx = 0; paramIdx < method.Parameters.Count; paramIdx++) {
                                        var param = method.Parameters[paramIdx];
                                        param.WriteTypeCheck(paramIdx, writer);
                                    }

                                    CallMethod(context, writer, qualifier, method, i);

                                }

                                writer._("break;");
                            }
                        }
                    }

                    foreach (var method in table.ParamsMethods) {
                        using (writer.B($"if (args.Length >= {method.RequiredParameterCount} && {CompareArguments(method)})")) {
                            CallMethod(context, writer, qualifier, method);
                        }
                    }

                    writer._();
                    var errorMessage = GetMethodNotMatchedErrorMessage($"{prototypeName}.{table.Name}: ", table);
                    writer._($"throw new InterpreterRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

                }
                writer._();
            }

        }

        writer._();

        // outerWriter.Write(w);

    }*/

}