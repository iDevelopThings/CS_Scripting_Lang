using System;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public partial class BindingsGenerator
{
    private void ModuleBindings(GeneratorExecutionContext context, INamedTypeSymbol module, Writer writer) {
        var moduleName   = module.GetAttributeArgument<string>(Attributes.Module, module.Name);
        var fnsAsGlobals = module.GetAttributeArgument<bool>(Attributes.Module, true, 1);

        var qualifier    = $"global::{module.GetFullyQualifiedName()}";
        var properties   = GetProperties(context, module).ToList();
        var methods      = GetMethods(context, module);
        var methodTables = MethodData.Build(context, methods);

        using (writer.B("public sealed partial class Library : ILibrary")) {

            if (!module.IsStatic) {
                var canDefaultConstruct = module.HasDefaultConstructor();

                writer._($"private readonly {qualifier} _instance;");
                writer._();
                writer._(canDefaultConstruct
                             ? $"public Library({qualifier} instance = null)"
                             : $"public Library({qualifier} instance)");
                writer.OpenBracket();
                writer._(canDefaultConstruct
                             ? $"_instance = instance ?? new {qualifier}();"
                             : $"_instance = instance ?? throw new ArgumentNullException(nameof(instance));");
                writer.CloseBracket();
                writer._();
            }

            using (writer.B("public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx)")) {
                var bindsCtorArgs = module.IsStatic ? "" : "_instance";
                writer._($"var binds = new Binds({bindsCtorArgs});");
                writer._();

                if (fnsAsGlobals) {
                    foreach (var property in properties) {
                        if (property.GetMethod != null) {
                            writer._($"yield return new KeyValuePair<string, Value>(\"get{property.Name}\", Value.Function(\"{property.Name}\", binds.{property.Name}__Getter));");
                        }

                        if (property.SetMethod != null) {
                            writer._($"yield return new KeyValuePair<string, Value>(\"set{property.Name}\", Value.Function(\"{property.Name}\", binds.{property.Name}__Setter));");
                        }
                    }

                    foreach (var table in methodTables) {
                        writer._($"yield return new KeyValuePair<string, Value>(\"{table.Identifier}\", Value.Function(\"{table.Identifier}\", binds.{table.Identifier}__Dispatch));");
                    }

                    writer._("yield break;");
                } else {
                    writer._("var result = Value.Object();");
                    writer._();

                    foreach (var property in properties) {
                        if (property.GetMethod != null) {
                            writer._($"result[\"get{property.Name}\"] = Value.Function(\"{property.Name}\", binds.{property.Name}__Getter);");
                        }

                        if (property.SetMethod != null) {
                            writer._($"result[\"set{property.Name}\"] = Value.Function(\"{property.Name}\", binds.{property.Name}__Setter);");
                        }
                    }

                    foreach (var table in methodTables) {
                        writer._($"result[\"{table.Identifier}\"] = Value.Function(\"{table.Identifier}\", binds.{table.Identifier}__Dispatch);");
                    }

                    writer._();

                    foreach (var table in methodTables) {
                        if (!table.IsGlobal)
                            continue;
                        writer._($"yield return new KeyValuePair<string, Value>(\"{table.Identifier}\", Value.Function(\"{table.Identifier}\", binds.{table.Identifier}__Dispatch));");
                    }

                    writer._($"yield return new KeyValuePair<string, Value>(\"{moduleName}\", result);");
                }
            }

            writer._();

            using (writer.B("private sealed class Binds")) {
                if (!module.IsStatic) {
                    writer._($"private readonly {qualifier} _instance;");
                    writer._();
                    using (writer.B($"public Binds({qualifier} instance)")) {
                        writer._("_instance = instance;");
                    }

                    writer._();
                }

                foreach (var property in properties) {
                    var propertyQualifier = property.Property.IsStatic ? qualifier : "_instance";

                    if (property.GetMethod != null) {
                        using (writer.B($"public Value {property.Name}__Getter(FunctionExecContext ctx, params Value[] args)")) {
                            using (writer.If("args.Length != 0")) {
                                writer._($"throw new InterpreterRuntimeException(\"{moduleName}.get{property.Name}: expected 0 arguments\");");
                            }

                            writer._($"var value = {propertyQualifier}.{property.Name};");
                            writer._($"return {ConvertToValue(context, "value", property.Type, property.Property)};");


                        }

                        writer._();
                    }

                    if (property.SetMethod != null) {
                        var parameter = Parameter.Create(context, property.SetMethod.Parameters[0]);

                        using (writer.B($"public Value {property.Name}__Setter(FunctionExecContext ctx, params Value[] args)")) {

                            using (writer.If($"args.Length != 1")) {
                                writer._($"throw new InterpreterRuntimeException(\"{moduleName}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                            }

                            if (parameter.RequiresParamTypeCheck()) {
                                using (writer.If($"!{CompareArgument(0, parameter)}")) {
                                    writer._($"throw new InterpreterRuntimeException(\"{moduleName}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                                }
                            }

                            writer._($"{propertyQualifier}.{property.Name} = {ConvertFromValue(context, 0, property.Type, property.Property)};");

                            writer._("return Value.Undefined;");
                        }

                        writer._();
                    }
                }

                foreach (var table in methodTables) {
                    using (writer.B($"public Value {table.Identifier}__Dispatch(FunctionExecContext ctx, params Value[] args)")) {

                        using (writer.B("switch (args.Length)")) {

                            for (var i = 0; i < table.Methods.Count; i++) {
                                var tableMethods = table.Methods[i];
                                if (tableMethods.Count == 0) {
                                    continue;
                                }

                                using (writer.B($"case {i}:")) {

                                    // var hasTrueType = false;

                                    foreach (var method in tableMethods) {
                                        var methodQualifier = method.Info.IsStatic ? qualifier : "_instance";

                                        for (var paramIdx = 0; paramIdx < method.Parameters.Count; paramIdx++) {
                                            var param = method.Parameters[paramIdx];
                                            param.WriteTypeCheck(paramIdx, writer);
                                        }

                                        CallMethod(context, writer, methodQualifier, method, i);

                                        /*var stmt = CompareArguments(method, out var isTrueType, i);
                                        if (isTrueType)
                                            hasTrueType = true;

                                        using (writer.If(stmt)) {
                                            CallMethod(context, writer, methodQualifier, method, i);
                                        }*/
                                    }

                                    // if (!hasTrueType)
                                    writer._("break;");
                                }
                            }

                        }

                        foreach (var method in table.ParamsMethods) {
                            var methodQualifier = method.Info.IsStatic ? qualifier : "_instance";
                            writer._($"if (args.Length >= {method.RequiredParameterCount} && {CompareArguments(method)})");
                            writer.OpenBracket();
                            CallMethod(context, writer, methodQualifier, method);
                            writer.CloseBracket();
                        }

                        writer._();
                        var errorPrefix = fnsAsGlobals
                            ? $"{table.Name}: "
                            : $"{moduleName}.{table.Name}: ";
                        var errorMessage = GetMethodNotMatchedErrorMessage(errorPrefix, table);
                        writer._($"throw new InterpreterRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

                    }

                    writer._();
                }
            }
        }


    }
}