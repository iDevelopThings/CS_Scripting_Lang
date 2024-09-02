using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Types;
using NUnit.Framework.Constraints;

namespace LanguageTests;

public class RuntimeValueEqualsConstraint : Constraint
{
    public RTVT   ExpectedType  { get; set; }
    public object ExpectedValue { get; set; }

    public RuntimeValueEqualsConstraint(RTVT expected) : base(expected) {
        Description = "The runtime value is equal to the expected value & matches the RTVT";

        ExpectedType = expected;
    }
    public RuntimeValueEqualsConstraint(object expectedValue, RTVT expectedType) : base(expectedValue, expectedType) {
        Description = "The runtime value is equal to the expected value & matches the RTVT";

        ExpectedValue = expectedValue;
        ExpectedType  = expectedType;
    }


    public override ConstraintResult ApplyTo<TActual>(TActual actual) {
        RuntimeValue actualValue = actual as RuntimeValue;
        if (actualValue == null) {
            return new ConstraintResult(this, actual, ConstraintStatus.Failure);
        }

        if (ExpectedValue != null) {
            if (actualValue.Value != ExpectedValue) {
                return new ConstraintResult(this, actual, ConstraintStatus.Failure);
            }
        }

        if (actualValue.Type != ExpectedType) {
            return new ConstraintResult(this, actual, ConstraintStatus.Failure);
        }

        return new ConstraintResult(this, actual, ConstraintStatus.Success);
    }
}

// nunit extensions
public class LangIs : NUnit.Framework.Is
{
    public static RuntimeValueEqualsConstraint RuntimeValue(RTVT expected) {
        return new RuntimeValueEqualsConstraint(expected);
    }
    public static RuntimeValueEqualsConstraint RuntimeValue(object expectedValue, RTVT expectedType) {
        return new RuntimeValueEqualsConstraint(expectedValue, expectedType);
    }
    public static RuntimeValueEqualsConstraint RuntimeValue(string                           expectedValue) => new(expectedValue, RTVT.String);
    public static RuntimeValueEqualsConstraint RuntimeValue(int                              expectedValue) => new(expectedValue, RTVT.Int32 | RTVT.Number);
    public static RuntimeValueEqualsConstraint RuntimeValue(long                             expectedValue) => new(expectedValue, RTVT.Int64 | RTVT.Number);
    public static RuntimeValueEqualsConstraint RuntimeValue(double                           expectedValue) => new(expectedValue, RTVT.Double | RTVT.Number);
    public static RuntimeValueEqualsConstraint RuntimeValue(float                            expectedValue) => new(expectedValue, RTVT.Float | RTVT.Number);
    public static RuntimeValueEqualsConstraint RuntimeValue(bool                             expectedValue) => new(expectedValue, RTVT.Boolean);
    public static RuntimeValueEqualsConstraint RuntimeValue(Dictionary<string, RuntimeValue> expectedValue) => new(expectedValue, RTVT.Object);
    public static RuntimeValueEqualsConstraint RuntimeValue(RuntimeValue_Object              expectedValue) => new(expectedValue, RTVT.Object);
    public static RuntimeValueEqualsConstraint RuntimeValue(RuntimeValue_Array               expectedValue) => new(expectedValue, RTVT.Array);
    public static RuntimeValueEqualsConstraint RuntimeValue(RuntimeValue_Function            expectedValue) => new(expectedValue, RTVT.Function);
}