using System.Globalization;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Number", RTVT.Number, typeof(NumberPrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(10)]
public partial class NumberPrototype : Prototype<NumberPrototype>
{
    public override bool IsPrimitive => true;

    public override Symbol Symbol => Symbol.For("Number");

    public override List<string> Aliases { get; set; } = [
        "number",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Number;

    public NumberPrototype(ExecContext ctx) : base(RTVT.Number, ctx) {
        Proto = Builder.Build(this, ctx, ValuePrototype.Instance);
    }
    
}

[LanguagePrototype("Int32", RTVT.Int32, typeof(NumberPrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(11)]
public partial class Int32Prototype : Prototype<Int32Prototype>
{
    public override Symbol Symbol => Symbol.For("Int32");

    public override List<string> Aliases { get; set; } = [
        "int", "int32", "i32",
    ];

    public override ZeroValueConstructor GetZeroValue() => args => Value.Int32(args.OfType<int>().FirstOrDefault());

    public Int32Prototype(ExecContext ctx) : base(RTVT.Int32, ctx) {
        Ty    = Types.Ty.Int32();
        Proto = Builder.Build(this, ctx, NumberPrototype.Instance, Ty);
    }

    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        return type switch {
            RTVT.Int32   => (true, (value => value)),
            RTVT.Int64   => (true, (value => Convert.ToInt64(value.GetUntypedValue<int>()))),
            RTVT.Float   => (true, (value => Convert.ToSingle(value.GetUntypedValue<int>()))),
            RTVT.Double  => (true, (value => Convert.ToDouble(value.GetUntypedValue<int>()))),
            RTVT.String  => (true, (value => value.GetUntypedValue<int>().ToString())),
            RTVT.Boolean => (true, (value => value.GetUntypedValue<int>() != 0)),
            _ => (false, null),
        };
    }
    
    [LanguageFunction]
    public static Value GetEnumerator(FunctionExecContext ctx, [LanguageInstance] Value inst) {
        inst.Is.ThrowIfNot(RTVT.Int32);

        var value = inst.As.Int();

        var enumerator = Value.Object(ctx);
        var i          = 0;

        enumerator["current"] = i;
        enumerator["moveNext"] = Value.Function("moveNext", (_, args) => {
            if (i >= value) {
                return false;
            }

            enumerator["current"] = i++;
            return true;
        });
        
        enumerator["dispose"] = Value.Function("dispose", (_, args) => Value.Null());
        
        return enumerator;
    }

}

[LanguagePrototype("Int64", RTVT.Int64, typeof(NumberPrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(11)]
public partial class Int64Prototype : Prototype<Int64Prototype>
{
    public override Symbol Symbol => Symbol.For("Int64");

    public override List<string> Aliases { get; set; } = [
        "long", "int64", "i64",
    ];

    public override ZeroValueConstructor GetZeroValue() => args => Value.Int64(args.OfType<int>().FirstOrDefault());

    public Int64Prototype(ExecContext ctx) : base(RTVT.Int64, ctx) {
        Ty    = Types.Ty.Int64();
        Proto = Builder.Build(this, ctx, NumberPrototype.Instance, Ty);
    }
    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        return type switch {
            RTVT.Int32   => (true, (value => Convert.ToInt32(value.GetUntypedValue<long>()))),
            RTVT.Int64   => (true, (value => value)),
            RTVT.Float   => (true, (value => Convert.ToSingle(value.GetUntypedValue<long>()))),
            RTVT.Double  => (true, (value => Convert.ToDouble(value.GetUntypedValue<long>()))),
            RTVT.String  => (true, (value => value.GetUntypedValue<long>().ToString())),
            RTVT.Boolean => (true, (value => value.GetUntypedValue<long>() != 0)),
            _ => (false, null),
        };
    }
    [LanguageFunction]
    public static Value GetEnumerator(FunctionExecContext ctx, [LanguageInstance] Value inst) {
        inst.Is.ThrowIfNot(RTVT.Int64);

        var value = inst.As.Int64();

        var enumerator = Value.Object(ctx);
        var i          = 0;

        enumerator["current"] = i;
        enumerator["moveNext"] = Value.Function("moveNext", (_, args) => {
            if (i >= value) {
                return false;
            }

            enumerator["current"] = i++;
            return true;
        });
        
        enumerator["dispose"] = Value.Function("dispose", (_, args) => Value.Null());
        
        return enumerator;
    }

}

[LanguagePrototype("Double", RTVT.Double, typeof(NumberPrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(11)]
public partial class DoublePrototype : Prototype<DoublePrototype>
{
    public override Symbol Symbol => Symbol.For("Double");

    public override List<string> Aliases { get; set; } = [
        "double", "f64",
    ];

    public override ZeroValueConstructor GetZeroValue() => args => Value.Double(args.OfType<int>().FirstOrDefault());

    public DoublePrototype(ExecContext ctx) : base(RTVT.Double, ctx) {
        Ty    = Types.Ty.Double();
        Proto = Builder.Build(this, ctx, NumberPrototype.Instance, Ty);
    }
    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        return type switch {
            RTVT.Int32   => (true, (value => Convert.ToInt32(value.GetUntypedValue<double>()))),
            RTVT.Int64   => (true, (value => Convert.ToInt64(value.GetUntypedValue<double>()))),
            RTVT.Float   => (true, (value => Convert.ToSingle(value.GetUntypedValue<double>()))),
            RTVT.Double  => (true, (value => value)),
            RTVT.String  => (true, (value => value.GetUntypedValue<double>().ToString(CultureInfo.InvariantCulture))),
            RTVT.Boolean => (true, (value => value.GetUntypedValue<double>() != 0)),
            _ => (false, null),
        };
    }
    
    [LanguageFunction]
    public static Value GetEnumerator(FunctionExecContext ctx, [LanguageInstance] Value inst) {
        inst.Is.ThrowIfNot(RTVT.Double);

        var value = inst.As.Double();

        var enumerator = Value.Object(ctx);
        var i          = 0;

        enumerator["current"] = i;
        enumerator["moveNext"] = Value.Function("moveNext", (_, args) => {
            if (i >= value) {
                return false;
            }

            enumerator["current"] = i++;
            return true;
        });
        
        enumerator["dispose"] = Value.Function("dispose", (_, args) => Value.Null());
        
        return enumerator;
    }

}

[LanguagePrototype("Float", RTVT.Float, typeof(NumberPrototype))]
[LanguageBindToModule("Prototypes")]
[PrototypeBoot(11)]
public partial class FloatPrototype : Prototype<FloatPrototype>
{
    public override Symbol Symbol => Symbol.For("Float");

    public override List<string> Aliases { get; set; } = [
        "float", "f32",
    ];

    public override ZeroValueConstructor GetZeroValue() => args => Value.Float(args.OfType<int>().FirstOrDefault());

    public FloatPrototype(ExecContext ctx) : base(RTVT.Float, ctx) {
        Ty    = Types.Ty.Float();
        Proto = Builder.Build(this, ctx, NumberPrototype.Instance, Ty);
    }
    
    public override (bool CanCast, Func<Value, Value> Cast) GetCaster(RTVT type) {
        return type switch {
            RTVT.Int32   => (true, (value => Convert.ToInt32(value.GetUntypedValue<float>()))),
            RTVT.Int64   => (true, (value => Convert.ToInt64(value.GetUntypedValue<float>()))),
            RTVT.Float   => (true, (value => value)),
            RTVT.Double  => (true, (value => Convert.ToDouble(value.GetUntypedValue<float>()))),
            RTVT.String  => (true, (value => value.GetUntypedValue<float>().ToString(CultureInfo.InvariantCulture))),
            RTVT.Boolean => (true, (value => value.GetUntypedValue<float>() != 0)),
            _ => (false, null),
        };
    }
    
    [LanguageFunction]
    public static Value GetEnumerator(FunctionExecContext ctx, [LanguageInstance] Value inst) {
        inst.Is.ThrowIfNot(RTVT.Float);

        var value = inst.As.Float();

        var enumerator = Value.Object(ctx);
        var i          = 0;

        enumerator["current"] = i;
        enumerator["moveNext"] = Value.Function("moveNext", (_, args) => {
            if (i >= value) {
                return false;
            }

            enumerator["current"] = i++;
            return true;
        });
        
        enumerator["dispose"] = Value.Function("dispose", (_, args) => Value.Null());
        
        return enumerator;
    }
}