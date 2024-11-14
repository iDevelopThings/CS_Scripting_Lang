using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using NUnit.Framework.Constraints;

namespace CSScriptingLang.Tests;

public static class ValueExtensions
{
    public static ValueAssertions Should(this Value instance) {
        return new ValueAssertions(instance);
    }
}

public class ValueAssertions : ReferenceTypeAssertions<Value, ValueAssertions>
{
    public ValueAssertions(Value instance) : base(instance) { }

    protected override string Identifier => "value";

    [CustomAssertion]
    public AndConstraint<ValueAssertions> IsType(RTVT type, string because = "", params object[] becauseArgs) {
        Execute.Assertion
           .BecauseOf(because, becauseArgs)
           .ForCondition(Subject?.Type == type)
           .FailWith("Expected {context:value} to be of type {0}, but found {1}.", type, Subject?.Type);

        return new AndConstraint<ValueAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<ValueAssertions> MatchRawValue(object value, string because = "", params object[] becauseArgs) {
        Execute.Assertion
           .BecauseOf(because, becauseArgs)
           .ForCondition(Subject != null)
           .FailWith("Expected {context:value} to be not null, but found null.")
           .Then
           .Given(() => Subject.GetUntypedValue())
           .ForCondition(v => v != null)
           .FailWith("Expected {context:value} to have a raw value, but found null.")
           .Then
           .ForCondition(v => v.Equals(value))
           .FailWith(
                "Expected {context:value} to be {0}, but found {1}.",
                value,
                Subject?.GetUntypedValue()
            );

        return new AndConstraint<ValueAssertions>(this);
    }
    [CustomAssertion]
    public AndConstraint<ValueAssertions> MatchValue(Value value, string because = "", params object[] becauseArgs) {
        Execute.Assertion
           .BecauseOf(because, becauseArgs)
           .ForCondition(Subject != null)
           .FailWith("Expected {context:value} to be not null, but found null.")
           .Then
           .Given(() => Subject)
           .ForCondition(v => v.Equals(value))
           .FailWith(
                "Expected {context:value} to be {0}, but found {1}.",
                value,
                Subject
            );

        return new AndConstraint<ValueAssertions>(this);
    }

}

public class Is : NUnit.Framework.Is
{
    public static ValueTypeConstraint ValueType(params object[] expected) {
        return new ValueTypeConstraint(expected);
    }
    public static ValueTypeConstraint ValueType(params Value[] expected) {
        return new ValueTypeConstraint(expected);
    }
}

public class ValueTypeConstraint : Constraint
{
    public List<object> ExpectedTypes      { get; set; } = new();
    public List<Value>  ExpectedValueTypes { get; set; } = new();
    public bool         IsValueType        { get; set; } = false;
    public bool         IsStrict           { get; set; } = false;

    private         string _description = string.Empty;
    public override string Description => _description;

    public ValueTypeConstraint(params Value[] args) : base(args) {
        ExpectedValueTypes = args.ToList();
        IsValueType        = true;
    }
    public ValueTypeConstraint(params object[] args) : base(args) {
        ExpectedTypes = args.ToList();
    }

    public ValueTypeConstraint Strict(bool v = true) {
        IsStrict = v;
        return this;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual) {
        if (IsValueType) {
            if (actual is not Value val) {
                _description = $"Expected a Value, got {actual.GetType()}";

                return new ConstraintResult(this, actual, false);
            }

            var expectedList = new Stack<Value>(ExpectedValueTypes);
            while (expectedList.Count > 0) {
                var value = expectedList.Pop();
                if (value.Operator_Equal(val, IsStrict)) {
                    return new ConstraintResult(this, actual, true);
                }
            }

            _description = $"Expected {string.Join(", ", ExpectedValueTypes.Select(v => v.Type))}, got {val.Type}";

        } else {

            var expectedList = new Stack<object>(ExpectedTypes);

            if (actual is not Value val) {
                return new ConstraintResult(this, actual, false);
            }

            while (expectedList.Count > 0) {
                var value = expectedList.Pop();
                if (value is Value v) {
                    value = v.GetUntypedValue();

                }

                var isMatch = (value, val.GetUntypedValue()) switch {
                    (int a, int b)       => a == b,
                    (long a, long b)     => a == b,
                    (float a, float b)   => a == b,
                    (double a, double b) => a == b,
                    (string a, string b) => a == b,
                    (bool a, bool b)     => a == b,

                    (int _, _)    => value.Equals(Convert.ToInt32(val.GetUntypedValue())),
                    (long _, _)   => value.Equals(Convert.ToInt64(val.GetUntypedValue())),
                    (float _, _)  => value.Equals(Convert.ToSingle(val.GetUntypedValue())),
                    (double _, _) => value.Equals(Convert.ToDouble(val.GetUntypedValue())),
                    (string _, _) => value.Equals(Convert.ToString(val.GetUntypedValue())),
                    (bool _, _)   => value.Equals(Convert.ToBoolean(val.GetUntypedValue())),

                    _ => throw new InvalidOperationException($"Unsupported type {value.GetType()}")
                };

                if (isMatch) {
                    return new ConstraintResult(this, actual, true);
                }

            }

        }

        return new ConstraintResult(this, actual, false);
    }

}

public static class ObjectExtensions
{
    public static PropertyAssertions<TSubject, TProperty> AndProp<TAssertions, TSubject, TProperty>(
        this ReferenceTypeAssertions<TSubject, TAssertions> that,
        Func<TSubject, TProperty>                           getter
    )
        where TAssertions : ReferenceTypeAssertions<TSubject, TAssertions>
        where TSubject : class {
        return new PropertyAssertions<TSubject, TProperty>(that.Subject, getter);
    }
    public static PropertyAssertions<TSubject, TProperty> AndProp<TAssertions, TSubject, TProperty>(
        this AndConstraint<TAssertions> that,
        Func<TSubject, TProperty>       getter
    )
        where TAssertions : ReferenceTypeAssertions<TSubject, TAssertions>
        where TSubject : class {
        return new PropertyAssertions<TSubject, TProperty>(that.And.Subject, getter);
    }
}

public static class SignalTestHelper
{
    public static List<Value> CaptureEmittedValues(Signal signal) {
        var capturedValues = new List<Value>();

        Action<Value[]> capturingListener = (values) => capturedValues.AddRange(values);
        signal.AddListener(capturingListener);

        return capturedValues;
    }
}

public static class SignalAssertions
{
    public static AndConstraint<GenericCollectionAssertions<Value>> EmitValues(
        this   ObjectAssertions signalAssertion,
        params Value[]          expectedValues
    ) {
        var signal = signalAssertion.Subject as Signal;
        if (signal == null) {
            Execute.Assertion.FailWith("Expected {context:signal} to be a Signal but found {0}.", signalAssertion.Subject);
        }

        var capturedValues = SignalTestHelper.CaptureEmittedValues(signal);

        Execute.Assertion
           .ForCondition(capturedValues.SequenceEqual(expectedValues))
           .FailWith("Expected signal to emit {0} but found {1}.", expectedValues, capturedValues);

        return new AndConstraint<GenericCollectionAssertions<Value>>(capturedValues.Should());
    }
}