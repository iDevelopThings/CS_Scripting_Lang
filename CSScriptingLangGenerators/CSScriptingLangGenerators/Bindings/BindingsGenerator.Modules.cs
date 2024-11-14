using System;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public partial class BindingsGenerator
{
    private void ModuleBindings(
        GeneratorExecutionContext context,
        INamedTypeSymbol          module,
        ModuleTypeData            moduleData
    ) {
        // var moduleData = ModuleTypeData.ForModuleBinding(module, context, outerWriter);
        var w = moduleData;

        var moduleName   = moduleData.Name;
        var fnsAsGlobals = moduleData.FunctionsAsGlobals;
        var qualifier    = moduleData.Qualifier;

        // var qualifier    = $"global::{module.GetFullyQualifiedName()}";
        // var properties   = GetProperties(context, module).ToList();
        // var methods      = GetMethods(context, module);
        // var methodTables = MethodData.Build(context, methods);

        using (w.B("public sealed partial class Library : ILibrary")) {

            if (!module.IsStatic) {
                var canDefaultConstruct = module.HasDefaultConstructor();

                w._($"public static {qualifier} GlobalInstance {{get;set;}}");
                w._($"public {qualifier} Instance {{get;set;}}");

                w._();
                using (w.B($"public Library({qualifier} inst {(canDefaultConstruct ? "= null" : "")})")) {
                    if (canDefaultConstruct) {
                        w._($"Instance = inst ?? new {qualifier}();");
                    } else {
                        w._($"Instance = inst ?? throw new ArgumentNullException(nameof(inst));");
                    }

                    w._($"GlobalInstance = Instance;");
                }
                w._();
            }

            using (w.B("public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx)")) {
                var bindsCtorArgs = module.IsStatic ? "" : "Instance";
                w._($"var binds = new Binds({bindsCtorArgs});");
                w._();

                moduleData.WriteDefinitions();


                if (fnsAsGlobals) {
                    /*foreach (var property in properties) {
                        if (property.GetMethod != null) {
                            w._($"yield return new KeyValuePair<string, Value>(" +
                                     $"\"get{property.Name}\", " +
                                     $"Value.Function(\"{property.Name}\", binds.{property.Name}__Getter)" +
                                     $");");
                        }

                        if (property.SetMethod != null) {
                            w._($"yield return new KeyValuePair<string, Value>(" +
                                     $"\"set{property.Name}\", " +
                                     $"Value.Function(\"{property.Name}\", binds.{property.Name}__Setter)" +
                                     $");");
                        }
                    }

                    foreach (var table in methodTables) {
                        w._($"yield return new KeyValuePair<string, Value>(\"{table.Identifier}\", Value.Function(\"{table.Identifier}\", binds.{table.Identifier}__Dispatch));");
                    }

                    w._("yield break;");*/
                } else {
                    /*
                    w._("var result = Value.Object();");
                    w._();

                    foreach (var property in properties) {
                        if (property.GetMethod != null) {
                            w._($"result[\"get{property.Name}\"] = Value.Function(" +
                                     $"\"{property.Name}\", binds.{property.Name}__Getter" +
                                     $");");
                        }

                        if (property.SetMethod != null) {
                            w._($"result[\"set{property.Name}\"] = Value.Function(\"{property.Name}\", binds.{property.Name}__Setter);");
                        }
                    }

                    foreach (var table in methodTables) {
                        w._($"result[\"{table.Identifier}\"] = Value.Function(\"{table.Identifier}\", binds.{table.Identifier}__Dispatch);");
                    }

                    w._();

                    foreach (var table in methodTables) {
                        if (!table.IsGlobal)
                            continue;
                        w._($"yield return new KeyValuePair<string, Value>(\"{table.Identifier}\", Value.Function(\"{table.Identifier}\", binds.{table.Identifier}__Dispatch));");
                    }

                    w._($"yield return new KeyValuePair<string, Value>(\"{moduleName}\", result);");
                    */
                }
            }

            w._();

            using (w.B("private sealed class Binds")) {
                if (!module.IsStatic) {
                    w._($"private readonly {qualifier} Instance;");
                    w._();
                    using (w.B($"public Binds({qualifier} inst)")) {
                        w._("Instance = inst;");
                    }

                    w._();
                }

                moduleData.WriteBindDefinitions();
                moduleData.WriteMethodDefinitions(moduleData.FunctionsAsGlobals, false);

                /*
                foreach (var property in properties) {
                    var propertyQualifier = property.Property.IsStatic ? qualifier : "_instance";

                    if (property.GetMethod != null) {
                        using (w.B($"public Value {property.Name}__Getter(FunctionExecContext ctx, params Value[] args)")) {
                            using (w.If("args.Length != 0")) {
                                w._($"throw new InterpreterRuntimeException(\"{moduleName}.get{property.Name}: expected 0 arguments\");");
                            }

                            w._($"var value = {propertyQualifier}.{property.Name};");
                            w._($"return {ConvertToValue(context, "value", property.Type, property.Property)};");


                        }

                        w._();
                    }

                    if (property.SetMethod != null) {
                        var parameter = Parameter.Create(context, property.SetMethod.Parameters[0]);

                        using (w.B($"public Value {property.Name}__Setter(FunctionExecContext ctx, params Value[] args)")) {

                            using (w.If($"args.Length != 1")) {
                                w._($"throw new InterpreterRuntimeException(\"{moduleName}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                            }

                            if (parameter.RequiresParamTypeCheck()) {
                                using (w.If($"!{CompareArgument(0, parameter)}")) {
                                    w._($"throw new InterpreterRuntimeException(\"{moduleName}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                                }
                            }

                            w._($"{propertyQualifier}.{property.Name} = {ConvertFromValue(context, 0, property.Type, property.Property)};");

                            w._("return Value.Undefined;");
                        }

                        w._();
                    }
                }
                */

                /*
                foreach (var table in methodTables) {
                    using (w.B($"public Value {table.Identifier}__Dispatch(FunctionExecContext ctx, params Value[] args)")) {

                        using (w.B("switch (args.Length)")) {

                            for (var i = 0; i < table.Methods.Count; i++) {
                                var tableMethods = table.Methods[i];
                                if (tableMethods.Count == 0) {
                                    continue;
                                }

                                using (w.B($"case {i}:")) {

                                    // var hasTrueType = false;

                                    foreach (var method in tableMethods) {
                                        var methodQualifier = method.Info.IsStatic ? qualifier : "_instance";

                                        for (var paramIdx = 0; paramIdx < method.Parameters.Count; paramIdx++) {
                                            var param = method.Parameters[paramIdx];
                                            param.WriteTypeCheck(paramIdx, w);
                                        }

                                        CallMethod(context, w, methodQualifier, method, i);

                                        /*var stmt = CompareArguments(method, out var isTrueType, i);
                                        if (isTrueType)
                                            hasTrueType = true;

                                        using (w.If(stmt)) {
                                            CallMethod(context, w, methodQualifier, method, i);
                                        }#1#
                                    }

                                    // if (!hasTrueType)
                                    w._("break;");
                                }
                            }

                        }

                        foreach (var method in table.ParamsMethods) {
                            var methodQualifier = method.Info.IsStatic ? qualifier : "_instance";
                            w._($"if (args.Length >= {method.RequiredParameterCount} && {CompareArguments(method)})");
                            w.OpenBracket();
                            CallMethod(context, w, methodQualifier, method);
                            w.CloseBracket();
                        }

                        w._();
                        var errorPrefix = fnsAsGlobals
                            ? $"{table.Name}: "
                            : $"{moduleName}.{table.Name}: ";
                        var errorMessage = GetMethodNotMatchedErrorMessage(errorPrefix, table);
                        w._($"throw new InterpreterRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

                    }

                    w._();
                }*/
            }
        }


        // outerWriter.Write(w);
    }
}