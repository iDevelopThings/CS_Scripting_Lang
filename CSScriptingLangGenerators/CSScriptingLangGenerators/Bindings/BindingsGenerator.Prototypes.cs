using System;
using System.Collections.Generic;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public partial class BindingsGenerator
{
    private static List<(IMethodSymbol Method, string Name, string Identifier)> GetInstanceGetters(GeneratorExecutionContext context, INamedTypeSymbol klass, bool? isStatic = null) {
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


    private static void PrototypeBindings(GeneratorExecutionContext context, INamedTypeSymbol prototype, Writer writer) {
        var prototypeName = prototype.GetAttributeArgument<string>(Attributes.Prototype, prototype.Name);
        var properties    = GetProperties(context, prototype, true).ToList();
        var methods       = GetMethods(context, prototype, true);

        var methodTables = MethodData.Build(
            context, methods
            // methods.Concat(instanceGetters.Select(x => (x.Method, x.Name, x.Identifier)))
        );

        var instanceGetters = GetInstanceGetters(context, prototype, true);
        var instanceTables  = MethodData.Build(context, instanceGetters);

        using (writer.B("private sealed class PrototypeObject")) {

            using (writer.B($"public static Value BuildTo(Value obj, {prototype} protoDef, Value basePrototype = null)")) {
                
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

            using (writer.B($"public static Value Build({prototype} protoDef, Value basePrototype = null)")) {
                writer._("var result = Value.Object();");
                writer._();
                writer._("result = BuildTo(result, protoDef, basePrototype);");
                writer._("result.Lock();");
                writer._("return result;");
            }

            writer._();

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
                using (writer.B($"private static Value {table.Identifier}__Dispatch(FunctionExecContext ctx, Value instance, params Value[] args)")) {

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
    }
}