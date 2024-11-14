using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

[DebuggerDisplay("PrototypeTypeData: {Name}")]
public class PrototypeTypeData : ClassTypeData
{
    public int RTVT { get; set; }

    public INamedTypeSymbol SuperPrototype { get; set; }

    public PrototypeTypeData(INamedTypeSymbol klass, GeneratorExecutionContext context, Writer writer) : base(klass, context, writer) { }

    public static PrototypeTypeData ForPrototypeBinding(INamedTypeSymbol prototype, GeneratorExecutionContext context, Writer writer) {
        var data = new PrototypeTypeData(prototype, context, writer) {
            Name           = prototype.GetAttributeArgument<string>(Attributes.Prototype, prototype.Name),
            RTVT           = prototype.GetAttributeArgument<int>(Attributes.Prototype, 0, 1),
            SuperPrototype = prototype.GetAttributeArgumentWithCompilation<INamedTypeSymbol>(Attributes.Prototype, context.Compilation, 2),

            AllowMethodInstanceType       = AllowedInstanceType.Static,
            AllowedConstructorMethodKinds = [MethodKind.Constructor, MethodKind.Ordinary],
            AllowPropertyInstanceType     = AllowedInstanceType.Static,
        };

        data.Meta = new TypeMeta_Prototype() {
            Name      = data.Name,
            Module    = data.BoundToModule,
            SuperType = data.SuperPrototype?.GetAttributeArgument<string>(Attributes.Prototype, prototype.Name),
            Namespace = prototype.GetFullyQualifiedName(),
            Kind      = TypeMetaKind.Prototype,
        };

        data.Load();

        return data;
    }

    public void WriteDefinitions() {

        using var mb = PropertyWriter("MainBuilder")
           .WithAccessibility(Accessibility.Public)
           .Static()
           .WithType("PrototypeObject")
           .WithValue($"new PrototypeObject()");

        using var b = PropertyWriter("Builder")
           .WithAccessibility(Accessibility.Public)
           .WithType("PrototypeObject")
           .WithValue($"MainBuilder");

        // Only write if the prototype doesn't have a default constructor
        if (Symbol.GetConstructors().Any(c => c.Parameters.Length == 0)) {
            _($"public {Symbol.Name}() {{ throw new InterpreterRuntimeException($\"{{GetType().Name}} cannot be constructed without an ExecContext\"); }}");
        }

    }

    public void WriteBuildToMethod() {
        using (B(
                   "public Value BuildTo(" +
                   "Value obj, " +
                   "Prototype protoDef, " +
                   "Value basePrototype = null, " +
                   "Ty type = null" +
                   ")"
               )) {

            _($"obj[\"symbolName\"] = Value.String(protoDef.Symbol.Name);");

            WritePropertyBindings("obj", null, true, "type", true);

            _();

            WriteGetters("obj", InstanceGettersTable);

            /*
            foreach (var property in Properties) {
                if (property.GetMethod != null) {
                    _($"obj[\"get{property.Name}\"] = Value.Function(\"{property.Name}\", {property.Name}__Getter);");
                }

                if (property.SetMethod != null) {
                    _($"obj[\"set{property.Name}\"] = Value.Function(\"{property.Name}\", {property.Name}__Setter);");
                }
            }
            */

            _();

            /*
            foreach (var table in instanceTables) {

                var method = table.GetFirstMethod();

                // _($"var value = {qualifier}.{property.Name};");
                // _($"return {ConvertToValue(context, "value", property.Type, property.Property)};");
                using (B($"Value __get_{method.Identifier}(ExecContext ctx, Value instance)")) {
                    _(
                        $"var result = {method.Info.ContainingType.GetFullyQualifiedName()}.{method.Info.Name}(",
                        method.GetArgumentBindings(context, 100).Select(b => $"{GetIndentString()}{b}").Join(", \n"),
                        ");",
                        $"return {ConvertToValue(context, "result", method.Info.ReturnType, method.Info)};"
                    );

                }
                _($"obj[\"{method.Identifier}\"] = Value.InstanceGetterFunction(\"{method.Identifier}\", __get_{method.Identifier});");
            }
            */

            /*
            _();

            foreach (var table in methodTables) {
                _($"obj[\"{table.Identifier}\"] = Value.Function(\"{table.Identifier}\", {table.Identifier}__Dispatch);");
            }
            */

            _();

            using (B("if (basePrototype != null)")) {
                _("obj.Prototype = basePrototype;");
            }

            _();

            _("return obj;");
        }
    }

    public void WriteBuildMethod() {
        using (B($"public Value Build(Prototype protoDef, ExecContext ctx, Value basePrototype = null, Ty type = null)")) {
            _("var result = Value.Object(ctx);");
            _();
            _("result = BuildTo(result, protoDef, basePrototype, type);");
            _("result.Lock();");
            _("return result;");
        }
    }

    public void WriteGetDefinitions() {
        using (B("public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx)")) {
            using var __ = YieldBlock();

            foreach (var table in MethodTable) {
                if (!table.IsGlobal)
                    continue;

                WriteDefinition(
                    table.Identifier,
                    table.Identifier,
                    $"{table.Identifier}__Dispatch"
                );
            }

            if (Constructors.Count > 0) {
                WriteDefinition(
                    Name,
                    $"{Name}__ctor",
                    "__ctor__Dispatch"
                );
            }

        }

    }


}