using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public partial class ValueSignal : BaseValue<ValueSignal, SignalDeclarationNode>
{
    public override RTVT Type             => RTVT.Signal;
    
    
    public new RuntimeTypeInfo_Signal RuntimeType {
        get => (RuntimeTypeInfo_Signal) base.RuntimeType;
        set => base.RuntimeType = value;
    }
    public List<ValueFunction> Listeners = new();

    public static   object GetNativeZero() => null;
    public override bool   IsZeroValue()   => Value == null;

    public ValueSignal() { }
    public ValueSignal(VariableSymbol symbol, SignalDeclarationNode value) : base(value) {
        Symbol      = symbol;
        RuntimeType = value.Type as RuntimeTypeInfo_Signal ?? throw new Exception("Signal declaration must have a type.");
    }
    public ValueSignal(SignalDeclarationNode value) : base(value) { }
    public ValueSignal(RuntimeTypeInfo_Signal value) : base(value) { }

    public static explicit operator ValueSignal(SignalDeclarationNode value) => new ValueSignal(value);
    public static explicit operator SignalDeclarationNode(ValueSignal value) => value.Value;

    public void AddListener(BaseValue runtimeValue) {
        var func = runtimeValue as ValueFunction;
        if (func == null) {
            throw new Exception("Signal listener must be a function.");
        }

        Listeners.Add(func);
    }

    public void RemoveListener(BaseValue runtimeValue) {
        var func = runtimeValue as ValueFunction;
        if (func == null) {
            throw new Exception("Signal listener must be a function.");
        }

        // Remove the listener by it's `.RuntimeType` property
        Listeners.RemoveAll(x => x.RuntimeType == func.RuntimeType);

    }


    [NativeFunctionBind]
    public void Emit(ref NativeFunctionExecutionContext ctx, ValueSignal @this, params object[] args) {
        if (args.Length != RuntimeType.Parameters.Count) {
            throw new Exception("Invalid number of arguments for signal emit.");
        }

        /*
        foreach (var listener in Listeners) {
            // listener.Call(args);
            var fnContext = new FunctionExecContext(ctx.Ctx) {
                Function = listener.Value,
                This     = @this
            };
            ctx.Ctx.Interpreter.ExecuteFunctionCall(fnContext, args.Select(x => (BaseValue) x).ToArray());
        }*/
    }

    public override BaseValue Operator_PlusEquals(BaseValue right) {
        if (right is ValueFunction func) {
            AddListener(func);
            return this;
        }

        return base.Operator_PlusEquals(right);
    }
    public override BaseValue Operator_MinusEquals(BaseValue right) {
        if (right is ValueFunction func) {
            RemoveListener(func);
            return this;
        }

        return base.Operator_MinusEquals(right);
    }
    
    public static ValueSignal Make()                             => new();
    public static ValueSignal Make(RuntimeTypeInfo_Signal value) => new(value);
    public static ValueSignal Make(SignalDeclarationNode  value) => new(value);
    public static ValueSignal Make(object                 value) => value == null ? Make() : new ValueSignal((SignalDeclarationNode) value);
}