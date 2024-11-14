using System.Diagnostics;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Mixins;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Prototypes;

public class StructPrototypeDebugView
{
    private readonly StructPrototype v;

    public StructPrototypeDebugView(StructPrototype value) {
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

[LanguagePrototype("Struct", RTVT.Struct, typeof(ObjectPrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(4)]
[DebuggerTypeProxy(typeof(StructPrototypeDebugView))]
[AddMixin(typeof(PrototypeMembersMixin))]
public partial class StructPrototype : Prototype<StructPrototype>, IPrototypeMembers
{
    public override bool IsPrimitive => false;

    public override Symbol Symbol { get; set; } = Symbol.For("Struct");

    public override List<string> Aliases { get; set; } = [
        "struct",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Struct;

    /*
    public class MemberMeta
    {
        public TypeDeclarationMemberNode Declaration { get; set; }

        public string                               Name       => Declaration.Name;
        public TypeDeclarationMemberNode.MemberType MemberType => Declaration.Type;

        public IEnumerable<AttributeDeclaration> Attributes => Declaration.Attributes;

        public class JsonAttribute
        {
            public string               Name        { get; set; }
            public AttributeDeclaration Declaration { get; set; }
        }

        public List<JsonAttribute> JsonAttributes   { get; set; }
        public Func<Value>         ValueConstructor { get; set; }

        public MemberMeta(TypeDeclarationMemberNode declaration) {
            Declaration = declaration;

            JsonAttributes = Attributes
               .Where(attr => attr.Name.Name.Equals("jsonable", StringComparison.CurrentCultureIgnoreCase))
               .Select(attr => {
                    var pName = declaration.Name.Name;
                    foreach (var arg in attr.Args) {
                        if (arg is not StringExpression str) {
                            throw new InterpreterRuntimeException("Expected a string argument for Jsonable attribute");
                        }
                        pName = str.RTValue;
                    }
                    return new JsonAttribute {
                        Name        = pName,
                        Declaration = attr,
                    };
                })
               .ToList();
        }
    }

    public List<MemberMeta>        DeclaredMembers      { get; set; } = new();
    public IEnumerable<MemberMeta> DeclaredFields       => DeclaredMembers.Where(m => m.MemberType == TypeDeclarationMemberNode.MemberType.Field);
    public IEnumerable<MemberMeta> DeclaredMethods      => DeclaredMembers.Where(m => m.MemberType == TypeDeclarationMemberNode.MemberType.Method);
    public IEnumerable<MemberMeta> DeclaredConstructors => DeclaredMembers.Where(m => m.MemberType == TypeDeclarationMemberNode.MemberType.Constructor);
    */

    public StructPrototype(ExecContext ctx) : base(RTVT.Struct, ctx) {
        Ty    = Types.Ty.Struct();
        Proto = Builder.Build(this, ctx, ObjectPrototype.Instance, Ty);
    }

    public MemberMeta FindConstructorForArgs(params Value[] args) {
        foreach (var ctor in DeclaredConstructors) {
            if (ctor.Declaration != null) {
                var ctorArgs = ctor.Declaration.FunctionDeclaration.Parameters;
                if (ctorArgs.Count != args.Length) {
                    continue;
                }

                var argsZipped = ctorArgs.Zip(args).ToList();
                if (argsZipped.All(pair => pair.First.TypeIdentifier.ResolveType().Type == pair.Second.Type)) {
                    return ctor;
                }
            }
            if (ctor.Decl != null) {
                var ctorArgs = ctor.Decl.FunctionDecl.Arguments;
                if (ctorArgs.Count() != args.Length) {
                    continue;
                }

                var argsZipped = ctorArgs.Zip(args).ToList();
                if (argsZipped.All(pair => pair.First.Type.ResolveType().Type == pair.Second.Type)) {
                    return ctor;
                }
            }
        }

        return null;
    }


    // Child constructor
    public StructPrototype(
        string          name,
        Value           value,
        StructPrototype parent,
        string          fqn = null
    ) {
        Symbol = Symbol.For(name);
        if (fqn != null)
            FQN = fqn;
        Rtvt = RTVT.Struct;

        Builder = parent.Builder;
        Proto   = value;

        ValueType = new(Rtvt, this);
    }

    public static StructPrototype MakeChild(
        ExecContext     ctx,
        string          name,
        Value           obj    = null,
        string          fqn    = null,
        StructPrototype parent = null
    ) {
        var p = (parent ?? Instance);

        obj ??= Value.Struct(ctx, p.ValueType.PrototypeInstance.Proto);

        fqn ??= name;

        var proto = new StructPrototype(name, obj, p, fqn) {
            Aliases = [name, fqn],
        };
        proto.Aliases = proto.Aliases.Distinct().ToList();
        proto.Ty      = Types.Ty.Struct();
        proto.Ty.Name = name;

        p.Builder.BuildTo(obj, p, null, proto.Ty);

        // proto.ValueType.PrototypeInstance.Proto

        obj.SetSymbol(name);

        return proto;
    }


    [LanguageValueConstructor]
    public static Value StructValueCtor(FunctionExecContext ctx, [LanguageInstance] Value instance, params Value[] args) {
        if (instance is not ValueType type) {
            throw new InterpreterRuntimeException($"Expected a struct type, got {instance}");
        }

        var obj         = Value.Struct(ctx, type.PrototypeInstance);
        var structProto = (type.PrototypeInstance as StructPrototype)!;
        obj.Prototype     = structProto.Proto;
        obj.PrototypeType = structProto;


        foreach (var member in structProto.DeclaredMembers) {
            // var memberValue = structProto.Proto[member.Name];

            /*if (memberValue.Type == RTVT.Array) {
                var arr = Value.Array(memberValue.As.Array().Select(e => e.GetOrClone()).ToList());
                obj[member.Name] = arr;
                continue;
            }*/
            // obj[member.Name] = memberValue.Clone();

            obj[member.Name] = member.ValueConstructor();
        }
        // structProto.DeclaredMembers.ForEach(member => {
        //     obj[member.Name] = structProto.Proto[member.Name].Clone();
        // });

        obj.SetSymbol(structProto.Symbol.Name);


        return obj;
    }
}