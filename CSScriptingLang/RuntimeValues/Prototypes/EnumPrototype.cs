using System.Diagnostics;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Mixins;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

public class EnumPrototypeDebugView
{
    private readonly EnumPrototype v;

    public EnumPrototypeDebugView(EnumPrototype value) {
        v = value;
    }
    public bool         IsPrimitive => v.IsPrimitive;
    public Symbol       Symbol      => v.Symbol;
    public List<string> Aliases     => v.Aliases;

    public string    FQN       => v.FQN;
    public Value     Proto     => v.Proto;
    public ValueType ValueType => v.ValueType;
    public RTVT      Rtvt      => v.Rtvt;

    public List<MemberMeta> DeclaredMembers => v.DeclaredMembers;
}

[LanguagePrototype("Enum", RTVT.Enum, typeof(ObjectPrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(4)]
[DebuggerTypeProxy(typeof(EnumPrototypeDebugView))]
[AddMixin(typeof(PrototypeMembersMixin))]
public partial class EnumPrototype : Prototype<EnumPrototype>, IPrototypeMembers
{
    public override bool IsPrimitive => false;

    public override Symbol Symbol { get; set; } = Symbol.For("Enum");

    public override List<string> Aliases { get; set; } = [
        "enum",
    ];

    public override ZeroValueConstructor GetZeroValue() => throw new NotImplementedException();

    public EnumPrototype(ExecContext ctx) : base(RTVT.Enum, ctx) {
        Ty    = Types.Ty.Enum();
        Proto = Builder.Build(this, ctx, ObjectPrototype.Instance, Ty);
    }

    // Child constructor
    public EnumPrototype(
        ExecContext   ctx,
        string        name,
        EnumPrototype parent,
        string        fqn = null
    ) {

        parent ??= Instance;

        Symbol = Symbol.For(name);
        if (fqn != null)
            FQN = fqn;
        Rtvt = RTVT.Enum;

        var value = Value.Object(ctx);
        value.SetSymbol(Symbol);
        value.Type = RTVT.Enum;

        Aliases = [name, fqn];
        Aliases = Aliases.Distinct().ToList();

        Builder = parent.Builder;
        Proto   = value;

        ValueType = new(Rtvt, this);

        Proto = Builder.BuildTo(value, this, parent.Proto);
        Proto.SetSymbol(Symbol);
        Proto.PrototypeType = this;


    }

    public Value MakeEnumMember(
        ExecContext ctx,
        MemberMeta  member
    ) {
        var obj = Value.Object(ctx);
        obj.SetSymbol(Symbol.Child(member.Name));
        obj.Type = RTVT.EnumMember;

        obj["name"] = Value.String(member.Name);

        // If the member has no constructors, it's a regular ordinal enum member
        if (!DeclaredConstructors.Any()) {
            if (member.Declaration != null)
                obj["value"] = member.Declaration.DefaultValues[0].Execute(ctx).Value;
            else if (member.Decl != null) {
                var defaults = member.Decl.DefaultValues.ToList();
                // If we have defined value
                if (defaults.Any())
                    obj["value"] = defaults.FirstOrDefault().DoExecuteSingle(ctx).Value().Value;
                else // otherwise we use the index(ordinal)
                    obj["value"] = Value.Int32(member.Ordinal);
            }

            obj.Prototype = obj["value"];
        } else {
            var ctor = DeclaredConstructors.First();

            if (ctor.Declaration != null) {
                var args = ctor.Declaration.FunctionDeclaration.Parameters
                   .Select(p => (p.Name.Name, p.TypeIdentifier))
                   .ToList();

                var argsZip = args.Zip(
                        member.Declaration.DefaultValues,
                        (a, v) => {
                            return new {
                                name  = a.Name,
                                type  = a.TypeIdentifier,
                                value = v.Execute(ctx).Value,
                            };
                        })
                   .ToList();

                var val = Value.Object(ctx);

                foreach (var v in argsZip) {
                    val[v.name] = v.value;
                }

                obj["value"] = val;

                obj.Prototype = obj["value"];
            }
            
            if (ctor.Decl != null) {
                var args = ctor.Decl.FunctionDecl.Arguments
                   .Select(p => (p.Name.AsString(), p.Type))
                   .ToList();

                var argsZip = args.Zip(
                        member.Decl.Arguments,
                        (a, v) => {
                            return new {
                                name  = a.Item1,
                                type  = a.Type,
                                value = v.DoExecuteSingle(ctx).Value().Value,
                            };
                        })
                   .ToList();

                var val = Value.Object(ctx);

                foreach (var v in argsZip) {
                    val[v.name] = v.value;
                }

                obj["value"] = val;

                obj.Prototype = obj["value"];
            }
            
        }


        return obj;
    }

    /*
    public static EnumPrototype MakeChild(
        ExecContext     ctx,
        string          name,
        Value           obj    = null,
        string          fqn    = null,
        EnumPrototype parent = null
    ) {
        var p = (parent ?? Instance);

        obj ??= Value.Enum(ctx, p.ValueType.PrototypeInstance.Proto);

        fqn ??= name;

        var proto = new EnumPrototype(name, obj, p, fqn) {
            Aliases = [name, fqn],
        };
        proto.Aliases = proto.Aliases.Distinct().ToList();

        p.Builder.BuildTo(obj, p);

        // proto.ValueType.PrototypeInstance.Proto

        obj.SetSymbol(name);

        return proto;
    }
    */


    /*
    [LanguageValueConstructor]
    public static Value EnumValueCtor(FunctionExecContext ctx, [LanguageInstance] Value instance, params Value[] args) {
        if (instance is not ValueType type) {
            throw new InterpreterRuntimeException($"Expected a struct type, got {instance}");
        }

        var obj         = Value.Enum(ctx, type.PrototypeInstance);
        var structProto = (type.PrototypeInstance as EnumPrototype)!;
        obj.Prototype     = structProto.Proto;
        obj.PrototypeType = structProto;


        foreach (var member in structProto.DeclaredMembers) {
            // var memberValue = structProto.Proto[member.Name];

            /*if (memberValue.Type == RTVT.Array) {
                var arr = Value.Array(memberValue.As.Array().Select(e => e.GetOrClone()).ToList());
                obj[member.Name] = arr;
                continue;
            }#1#
            // obj[member.Name] = memberValue.Clone();

            obj[member.Name] = member.ValueConstructor();
        }
        // structProto.DeclaredMembers.ForEach(member => {
        //     obj[member.Name] = structProto.Proto[member.Name].Clone();
        // });

        obj.SetSymbol(structProto.Symbol.Name);


        return obj;
    }*/
}