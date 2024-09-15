using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public class StructDictionary : Dictionary<string, BaseValue> { }

public partial class ValueStruct : BaseValue<ValueStruct, StructDictionary>
{
    public override RTVT Type             => RTVT.Struct;
    
    public new RuntimeTypeInfo_Struct RuntimeType {
        get => (RuntimeTypeInfo_Struct) base.RuntimeType;
        set => base.RuntimeType = value;
    }
    public override bool ToBool()      => true;
    public override bool IsZeroValue() => false;

    public ValueStruct() {
        Value = new StructDictionary();
    }
    public ValueStruct(RuntimeTypeInfo_Struct type) : base(type) {
        Value = new StructDictionary();

        foreach (var field in type.Fields) {
            SetField(field.Key, field.Value.ValueConstructor());
        }
    }
    public ValueStruct(StructDictionary value) : base(value) { }

    public static explicit operator ValueStruct(StructDictionary value) => new ValueStruct(value);
    public static explicit operator StructDictionary(ValueStruct value) => value.Value;

    public static object GetNativeZero() => null;

    public static ValueStruct Make()                             => new();
    public static ValueStruct Make(RuntimeTypeInfo_Struct value) => new(value);
    public static ValueStruct Make(StructDictionary       value) => new(value);
    public static ValueStruct Make(object                 value) => value == null ? Make() : new ValueStruct((StructDictionary) value);

    /*[NativeFunctionBind("new")]
    public static BaseValue New(ref NativeFunctionExecutionContext ctx) {
        var caller    = ctx.Ctx.CurrentCallFrame.ReturnNode;
        var typeParam = caller.TypeParameters?.FirstOrDefault();

        if (typeParam == null) {
            throw new InterpreterException("Struct type not specified", caller, caller.GetScript());
        }

        var structType = TypeTable.Current.Get(typeParam.Name);
        if (structType == null) {
            throw new InterpreterException($"Struct type '{typeParam.Name}' not found", caller, caller.GetScript());
        }

        var ctor = structType.GetValueConstructor();
        
        var value = ctor.Invoke(ctx.Ctx, ctx.Ctx.Params.Select(p => p.Value).ToArray());
        
        return value;
    }*/


    public override StructDictionary GetValue() {
        if (GetterProxy != null) {
            return (StructDictionary) GetterProxy(this);
        }

        return (StructDictionary) Fields;
    }

    public override void SetValue(StructDictionary value) {
        if (SetterProxy != null) {
            SetterProxy(this, value);
            return;
        }

        Fields = value;
        _value = value;
    }
}