using System.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Values;

[NoGeneratedConversionOperators]
[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public partial class ValueFunction : BaseValue<ValueFunction, InlineFunctionDeclaration>
{
    public override RTVT Type => RTVT.Function;
    public new RuntimeTypeInfo_Function RuntimeType {
        get => (RuntimeTypeInfo_Function) base.RuntimeType;
        set => base.RuntimeType = value;
    }

    public delegate Value ExecutableFunction(FunctionExecContext ctx, params Value[] arguments);
    public delegate Value ExecutableInstanceFunction(FunctionExecContext ctx, Value instance, params Value[] arguments);
    public delegate Value ExecutableInstanceGetterFunction(ExecContext   ctx, Value instance);

    public ExecutableFunction         Executable         { get; set; }
    public ExecutableInstanceFunction InstanceExecutable { get; set; }

    public class Parameter
    {
        public string      Name         { get; set; }
        public RuntimeType Type         { get; set; }
        public BaseValue   DefaultValue { get; set; } = ValueFactory.Null.Make();
        public bool        IsOptional   { get; set; }

        public ArgumentDeclarationNode Declaration { get; set; }
    }

    public List<Parameter> Parameters { get; set; } = new();

    public string FunctionName => Value?.Name ?? "Anonymous Function";

    public ValueFunction() { }
    /*public ValueFunction(ValueFunction value) : base(value.Value) {
        Executable         = value.Executable;
        InstanceExecutable = value.InstanceExecutable;
        Parameters         = value.Parameters;
    }*/
    public ValueFunction(ExecutableFunction fn, string name = null) {
        Executable = fn;

        if (name != null) {
            Value = new InlineFunctionDeclaration {
                Name = name,
            };
        }
    }
    public ValueFunction(ExecutableInstanceFunction fn, string name = null) {
        InstanceExecutable = fn;

        if (name != null) {
            Value = new InlineFunctionDeclaration {
                Name = name,
            };
        }
    }
    public ValueFunction(InlineFunctionDeclaration value) : base(value) {
        Value = value;
    }
    public ValueFunction(RuntimeTypeInfo_Function value) : base(value) {
        if (Value == null) {
            if (value?.LinkedNode is InlineFunctionDeclaration inlineFunction)
                Value = inlineFunction;
        }

        SetParameters();
    }


    private void SetParameters() {
        if (RuntimeType == null)
            return;
        foreach (var parameter in RuntimeType.Parameters) {
            Parameters.Add(new Parameter {
                Name = parameter.Name,
                Type = parameter.Type,
            });
        }
    }

    public static   object GetNativeZero() => null;
    public override bool   IsZeroValue()   => Value == null;
    public override bool   ToBool()        => Value != null;

    public override string ToDebugString() {
        return $"{GetType().ToShortName()} {FunctionName}";
    }

    public static ValueFunction Make()                                    => new();
    public static ValueFunction Make(RuntimeTypeInfo_Function      value) => new(value);
    public static ValueFunction Make(InlineFunctionDeclaration value) => new(value);

    public static ValueFunction Make(ExecutableFunction value)                          => new(value);
    public static ValueFunction Make(string             name, ExecutableFunction value) => new(value, name);

    public static ValueFunction Make(ExecutableInstanceFunction value)                                  => new(value);
    public static ValueFunction Make(string                     name, ExecutableInstanceFunction value) => new(value, name);

    public static ValueFunction Make(object value) => value == null ? Make() : new ValueFunction((InlineFunctionDeclaration) value);

    public override bool CanCastTo<T>() => CanCastTo(typeof(T));

    public override bool CanCastTo(Type t) {
        if (base.CanCastTo(t)) return true;
        if (t == typeof(ValueBoolean)) return true;
        return false;
    }

    public override T CastTo<T>() => (T) CastTo(typeof(T));

    public override BaseValue CastTo(Type t) {
        var baseResult = base.CastTo(t);
        if (baseResult != null) return baseResult;
        if (t == typeof(ValueFunction)) return this;
        if (t == typeof(ValueBoolean)) return ValueFactory.Boolean.Make(Value != null);
        throw new ArgumentException($"Cannot cast Function to {t}");
    }

    public bool TryCall(FunctionExecContext ctx, Value instance, out Value returnValue, params Value[] args) {
        if (Executable != null) {
            returnValue = Executable.Invoke(ctx, args);
            return true;
        }

        if (InstanceExecutable != null) {
            returnValue = InstanceExecutable.Invoke(ctx, instance, args);
            return true;
        }

        throw new InterpreterRuntimeException($"Function has no executable {Value}");
    }
}