using CSScriptingLang.RuntimeValues.Values;
using NUnit.Framework.Constraints;

namespace LanguageTests;

public class Is : NUnit.Framework.Is
{
    public static ValueTypeConstraint ValueType(params object[] expected) {
        return new ValueTypeConstraint(expected);
    }
}

public class ValueTypeConstraint : Constraint
{
    public List<object> ExpectedTypes { get; set; } = new();

    public ValueTypeConstraint(params object[] args) : base(args) {
        ExpectedTypes = args.ToList();
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual) {
        var expectedList = new Stack<object>(ExpectedTypes);

        if(actual is not Value val) {
            return new ConstraintResult(this, actual, false);
        }
        
        while (expectedList.Count > 0) {
            var value = expectedList.Pop();

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

        return new ConstraintResult(this, actual, false);
    }
}