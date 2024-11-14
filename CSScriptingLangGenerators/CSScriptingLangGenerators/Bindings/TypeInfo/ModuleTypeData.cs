using System.Collections.Generic;
using System.Diagnostics;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

[DebuggerDisplay("ModuleTypeData: {Name}")]
public class ModuleTypeData : ClassTypeData
{
    public bool FunctionsAsGlobals { get; set; }

    public ModuleTypeData(INamedTypeSymbol klass, GeneratorExecutionContext context, Writer writer) : base(klass, context, writer) { }

    public static ModuleTypeData ForModuleBinding(INamedTypeSymbol klass, GeneratorExecutionContext context, Writer writer) {
        var data = new ModuleTypeData(klass, context, writer) {
            Name               = klass.GetAttributeArgument<string>(Attributes.Module, klass.Name),
            FunctionsAsGlobals = klass.GetAttributeArgument<bool>(Attributes.Module, true, 1),

            AllowedConstructorMethodKinds = [MethodKind.Constructor, MethodKind.Ordinary],
            AllowPropertyInstanceType     = AllowedInstanceType.Both,
            AllowMethodInstanceType       = AllowedInstanceType.Instance,
        };

        data.Meta = new TypeMeta_Module {
            Name      = data.Name,
            Namespace = klass.GetFullyQualifiedName(),
            Kind      = TypeMetaKind.Module,
        };

        data.Load();

        return data;
    }

    public void WriteDefinitions() {
        using var _ = YieldBlock();

        if (FunctionsAsGlobals) {
            WriteAsGlobal();
        } else {
            WriteAsModule();
        }

        if (Symbol.ImplementsInterface(TypeData.ILibraryImplInterface)) {
            using (B($"foreach(var kv in (({TypeData.ILibraryImplInterface.Name}) Instance).OnGetLibraryDefinitions(ctx, this))")) {
                yieldReturn("kv");
            }
            using (B($"foreach(var l in (({TypeData.ILibraryImplInterface.Name}) Instance).OnGetAdditionalLibraries(ctx, this))")) {
                // yieldReturn("l.GetDefinitions(ctx)");
                using (B("foreach(var kv in l.GetDefinitions(ctx))")) {
                    yieldReturn("kv");
                }
            }
        }

    }

    public void WriteBindDefinitions() {

        foreach (var property in Properties) {
            var propertyQualifier = property.Property.IsStatic ? Qualifier : "Instance";

            if (property.GetMethod != null) {
                using var mw = MethodWriter($"{property.Name}__Getter")
                   .Static(FunctionsAsGlobals)
                   .WithReturnType("Value")
                   .WithParameter("ctx", "FunctionExecContext")
                   .WithParameter("args", "params Value[]")
                   .Body(writer => {
                        using (writer.If("args.Length != 0")) {
                            writer._($"throw new InterpreterRuntimeException(\"{Name}.get{property.Name}: expected 0 arguments\");");
                        }

                        writer._($"var value = {propertyQualifier}.{property.Name};");
                        writer._($"return {BindingsGenerator.ConvertToValue(Context, "value", property.Type, property.Property)};");

                    });
                _();
            }


            if (property.SetMethod != null) {
                var parameter = Parameter.Create(Context, property.SetMethod.Parameters[0]);

                using var mw = MethodWriter($"{property.Name}__Setter")
                   .Static(FunctionsAsGlobals)
                   .WithReturnType("Value")
                   .WithParameter("ctx", "FunctionExecContext")
                   .WithParameter("args", "params Value[]")
                   .Body(writer => {

                        using (writer.If($"args.Length != 1")) {
                            writer._($"throw new InterpreterRuntimeException(\"{Name}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                        }

                        if (parameter.RequiresParamTypeCheck()) {
                            parameter.WriteTypeCheck(0, writer);
                            // using (writer.If($"!{CompareArgument(0, parameter)}")) {
                            //     writer._($"throw new InterpreterRuntimeException(\"{Name}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                            // }
                        }

                        writer._($"{propertyQualifier}.{property.Name} = {BindingsGenerator.ConvertFromValue(Context, 0, property.Type, property.Property)};");

                        writer._("return Value.Null();");

                    });

                _();
            }
        }


    }

    private void WriteAsModule() {
        _("var result = Value.Object(ctx);");
        _();

        WritePropertyBindings("result", "binds", true);

        _();

        foreach (var table in MethodTable) {
            if (!table.IsGlobal)
                continue;
            WriteDefinition(table.Identifier, table.Identifier, $"binds.{table.Identifier}__Dispatch");
        }

        _();

        WriteGetters("result", GettersTable);

        _();

        yieldReturn($"new KeyValuePair<string, Value>(\"{Name}\", result)");
    }

    private void WriteAsGlobal() {
        foreach (var property in Properties) {
            if (property.GetMethod != null) {
                WriteDefinition(property.BindingGetterName, property.Name, $"binds.{property.Name}__Getter");
            }

            if (property.SetMethod != null) {
                WriteDefinition(property.BindingSetterName, property.Name, $"binds.{property.Name}__Setter");
            }
        }

        foreach (var table in MethodTable) {
            WriteDefinition(table.Identifier, table.Identifier, $"binds.{table.Identifier}__Dispatch");
        }
    }

    protected override void WriteMethodChecks(string methodName, string instanceName = "instance", Writer w = null) {
        w ??= this;

        /*using (w.B($"if ({instanceName}.Type != RTVT.Object)")) {
            w._($"throw new InterpreterRuntimeException(\"{Name}.{methodName}: can only be called on an instance of {Name}\");");
        }

        if (Symbol.HasBaseType(TypeData.Value)) {
            using (w.B($"if ({instanceName} is not {Qualifier} obj)")) {
                w._($"throw new InterpreterRuntimeException(\"{Name}.{methodName}: can only be called on an instance of {Name}\");");
            }
        } else {
            using (w.B($"if ({instanceName}?.GetUntypedValue() is not {Qualifier} obj)")) {
                w._($"throw new InterpreterRuntimeException(\"{Name}.{methodName}: can only be called on an instance of {Name}\");");
            }
        }*/

        w._($"var obj = Instance;");

        w._();
    }


}