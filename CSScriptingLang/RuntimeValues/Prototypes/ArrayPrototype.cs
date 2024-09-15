using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

[LanguagePrototype("Array")]
[PrototypeBoot(8)]
public partial class ArrayPrototype : Prototype<ArrayPrototype>
{
    public override bool   IsPrimitive => false;
    public override Symbol Symbol      => Symbol.For("Array");

    public override List<string> Aliases { get; set; } = [
        "array",
    ];

    public override ZeroValueConstructor GetZeroValue() => Value.Array;

    public ArrayPrototype() : base(RTVT.Array, PrototypeObject.Build, ValuePrototype.Instance) { }

    [LanguageInstanceGetterFunction("length")]
    public static int GetLength(ExecContext ctx, [LanguageInstance] Value inst) {
        inst.Is.ThrowIfNot(RTVT.Array);
        return inst.As.Array().Count;
    }

    [LanguageFunction]
    public static int Push(FunctionExecContext ctx, [LanguageInstance] Value inst, params Value[] args) {
        inst.Is.ThrowIfNot(RTVT.Array);

        var arr = inst.As.Array();
        foreach (var arg in args) {
            arr.Add(arg);
        }

        return arr.Count;
    }

    [LanguageFunction]
    public static int RemoveAt(FunctionExecContext ctx, [LanguageInstance] Value inst, Value index) {
        inst.Is.ThrowIfNot(RTVT.Array);

        if (!index.Is.Number) {
            Logger.Warning($"Index must be a number, got {index.Type}");
            return -1;
        }

        inst.As.Array().RemoveAt(index.As.Int32());

        return inst.As.Array().Count;
    }

    [LanguageFunction]
    public static int RemoveRange(FunctionExecContext ctx, [LanguageInstance] Value inst, Value start, Value end) {
        inst.Is.ThrowIfNot(RTVT.Array);

        if (!start.Is.Number || !end.Is.Number) {
            Logger.Warning($"Start and end must be numbers, got {start.Type} and {end.Type}");
            return -1;
        }

        var count = end.As.Int32() - start.As.Int32();
        inst.As.Array().RemoveRange(start.As.Int32(), count);

        return inst.As.Array().Count;
    }

}