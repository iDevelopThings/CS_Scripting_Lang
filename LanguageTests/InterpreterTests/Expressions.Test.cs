using System.Collections;
using System.Diagnostics.CodeAnalysis;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Values;

namespace LanguageTests.InterpreterTests;

[TestFixture]
[SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
public class ExpressionsTest : BaseCompilerTest
{

    public static IEnumerable BasicExpressions {
        get {
            yield return new TestCaseData("1.0f + 2.0f", 3.0f);
            yield return new TestCaseData("1.0f - 2.0f", -1.0f);
            yield return new TestCaseData("1.0f * 2.0f", 2.0f);
            yield return new TestCaseData("1.0f / 2.0f", 0.5f);
            yield return new TestCaseData("1.0f % 2.0f", 1.0f);
            yield return new TestCaseData("1.0 + 2.0", 3.0);
            yield return new TestCaseData("1.0 - 2.0", -1.0);
            yield return new TestCaseData("1.0 * 2.0", 2.0);
            yield return new TestCaseData("1.0 / 2.0", 0.5);
            yield return new TestCaseData("1.0 % 2.0", 1.0);
            yield return new TestCaseData("1 + 2", 3);
            yield return new TestCaseData("1 - 2", -1);
            yield return new TestCaseData("1 * 2", 2);
            yield return new TestCaseData("1 / 2", 0);
            yield return new TestCaseData("1 % 2", 1);

        }
    }

    [Test]
    public void Basics_Value() {
        // ReSharper disable once RedundantAssignment
        var vals = (Value.Number(1.0f), Value.Number(2.0f));
        vals = (1.0f, 2.0f);

        var res = vals.Item1 + vals.Item2;
        Assert.That(vals.Item1 + vals.Item2, Is.EqualTo(Value.Number(3.0f)));
        Assert.That(vals.Item1 - vals.Item2, Is.EqualTo(Value.Number((-1.0f))));
        Assert.That(vals.Item1 * vals.Item2, Is.EqualTo(Value.Number(2.0f)));
        Assert.That(vals.Item1 / vals.Item2, Is.EqualTo(Value.Number(0.5f)));
        Assert.That(vals.Item1 % vals.Item2, Is.EqualTo(Value.Number(1.0f)));

        vals = (1, 2);
        Assert.That(vals.Item1 + vals.Item2, Is.EqualTo((Value) 3));
        Assert.That(vals.Item1 - vals.Item2, Is.EqualTo((Value) (-1)));
        Assert.That(vals.Item1 * vals.Item2, Is.EqualTo((Value) 2));
        Assert.That(vals.Item1 / vals.Item2, Is.EqualTo((Value) 0));
        Assert.That(vals.Item1 % vals.Item2, Is.EqualTo((Value) 1));

        vals = (1.0, 2.0);
        Assert.That(vals.Item1 + vals.Item2, Is.EqualTo((Value) 3.0));
        Assert.That(vals.Item1 - vals.Item2, Is.EqualTo((Value) (-1.0)));
        Assert.That(vals.Item1 * vals.Item2, Is.EqualTo((Value) 2.0));
        Assert.That(vals.Item1 / vals.Item2, Is.EqualTo((Value) 0.5));
        Assert.That(vals.Item1 % vals.Item2, Is.EqualTo((Value) 1.0));


    }

    [Test]
    [TestCaseSource(typeof(ExpressionsTest), nameof(BasicExpressions))]
    public void BasicsList(string expr, object expected) {
        var value = ExecuteSimpleExpression(expr);
        Assert.That(value, Is.ValueType(expected));
    }

    public static IEnumerable ComplexExpressionsSource {
        get {
            // Mixed type operations
            yield return new TestCaseData("1 + 2.0", () => Value.Number(1) + Value.Number(2.0f), Value.Number(3.0f));
            yield return new TestCaseData("1.0f + 2", () => Value.Number(1.0f) + Value.Number(2), Value.Number(3.0f));
            yield return new TestCaseData("1.0 + 2", () => Value.Number(1.0) + Value.Number(2), Value.Number(3.0));
            yield return new TestCaseData("2.0 + 1", () => Value.Number(2.0) + Value.Number(1), Value.Number(3.0));
            yield return new TestCaseData("2.0f + 1", () => Value.Number(2.0f) + Value.Number(1), Value.Number(3.0f));
            yield return new TestCaseData("1 + 2.0f", () => Value.Number(1) + Value.Number(2.0f), Value.Number(3.0f));
            yield return new TestCaseData("1 + 2 + 3.0", () => Value.Number(1) + Value.Number(2) + Value.Number(3.0), Value.Number(6.0));
            yield return new TestCaseData("1 + 2.0 + 3", () => Value.Number(1) + Value.Number(2.0) + Value.Number(3), Value.Number(6.0));

            // Nested expressions with parentheses
            yield return new TestCaseData("(1 + 2) * 3", () => (Value.Number(1) + Value.Number(2)) * Value.Number(3), Value.Number(9));
            yield return new TestCaseData("(1.0 + 2.0) * 3.0", () => (Value.Number(1.0) + Value.Number(2.0)) * Value.Number(3.0), Value.Number(9.0));
            yield return new TestCaseData("1 + (2 * 3)", () => Value.Number(1) + (Value.Number(2) * Value.Number(3)), Value.Number(7));
            yield return new TestCaseData("(1 + 2) * (3 + 4)", () => (Value.Number(1) + Value.Number(2)) * (Value.Number(3) + Value.Number(4)), Value.Number(21));
            yield return new TestCaseData("(1.0 + 2.0) * (3.0 + 4.0)", () => (Value.Number(1.0) + Value.Number(2.0)) * (Value.Number(3.0) + Value.Number(4.0)), Value.Number(21.0));

            // Complex mathematical expressions
            yield return new TestCaseData("(1.0f + 2.0f) / 3.0f", () => (Value.Number(1.0f) + Value.Number(2.0f)) / Value.Number(3.0f), Value.Number(1.0f));
            yield return new TestCaseData("1 + 2 * 3 - 4 / 2", () => Value.Number(1) + Value.Number(2) * Value.Number(3) - Value.Number(4) / Value.Number(2), Value.Number(5));
            yield return new TestCaseData("1.0 + 2.0 * 3.0 - 4.0 / 2.0", () => Value.Number(1.0) + Value.Number(2.0) * Value.Number(3.0) - Value.Number(4.0) / Value.Number(2.0), Value.Number(5.0));

            // Division by zero cases
            yield return new TestCaseData("1 / 0", () => Value.Number(1) / Value.Number(0), Value.Number(0));
            yield return new TestCaseData("1.0 / 0.0", () => Value.Number(1.0) / Value.Number(0.0), Value.Number(0.0));
            yield return new TestCaseData("1.0f / 0.0f", () => Value.Number(1.0f) / Value.Number(0.0f), Value.Number(0.0f));

            // String concatenation
            yield return new TestCaseData("\"hello\" + \" world\"", () => Value.String("hello") + Value.String(" world"), Value.String("hello world"));

            // Edge case: multiple types in one expression
            yield return new TestCaseData("1 + 2.0 + 3.0f", () => Value.Number(1) + Value.Number(2.0) + Value.Number(3.0f), Value.Number(6.0));
            yield return new TestCaseData("1.0 + 2 + 3.0f", () => Value.Number(1.0) + Value.Number(2) + Value.Number(3.0f), Value.Number(6.0));

            // Floating point precision
            yield return new TestCaseData("0.1 + 0.2", () => Value.Number(0.1) + Value.Number(0.2), Value.Number(0.3)); // Be aware of floating-point precision issues

            // Parentheses and precedence
            yield return new TestCaseData("3 * (2 + 1)", () => Value.Number(3) * (Value.Number(2) + Value.Number(1)), Value.Number(9));
            yield return new TestCaseData("(3 * 2) + 1", () => (Value.Number(3) * Value.Number(2)) + Value.Number(1), Value.Number(7));
            yield return new TestCaseData("(3 + 2) * (4 - 1)", () => (Value.Number(3) + Value.Number(2)) * (Value.Number(4) - Value.Number(1)), Value.Number(15));

        }
    }

    [Test]
    [TestCaseSource(typeof(ExpressionsTest), nameof(ComplexExpressionsSource))]
    public void ComplexExpressions(string expr, Func<Value> func, object expected) {
        var exprResult = ParseExpression(expr).Execute(Ctx);

        var operatorExprResult = func();
        Assert.That(operatorExprResult, Is.EqualTo(exprResult.Value));

        // Assert.That(exprResult, Is.ValueType(expected));
    }

    public class ExprTestCase : TestCaseData
    {
        public ExprTestCase(string expr, Func<Value> func, object expected) : base(expr, func, expected) { }
    }

    public static IEnumerable ConditionalExpressionsSource {
        get {
            // Bool expressions
            yield return new ExprTestCase("true && true", () => Value.Boolean(true) && Value.Boolean(true), Value.Boolean(true));
            yield return new ExprTestCase("true && false", () => Value.Boolean(true) && Value.Boolean(false), Value.Boolean(false));
            yield return new ExprTestCase("false && true", () => Value.Boolean(false) && Value.Boolean(true), Value.Boolean(false));
            yield return new ExprTestCase("false && false", () => Value.Boolean(false) && Value.Boolean(false), Value.Boolean(false));

            // Int expressions
            yield return new ExprTestCase("1 && 1", () => Value.Number(1) && Value.Number(1), Value.Boolean(true));
            yield return new ExprTestCase("1 && 0", () => Value.Number(1) && Value.Number(0), Value.Boolean(false));
            yield return new ExprTestCase("0 && 1", () => Value.Number(0) && Value.Number(1), Value.Boolean(false));
            yield return new ExprTestCase("0 && 0", () => Value.Number(0) && Value.Number(0), Value.Boolean(false));

            // Bool expressions
            yield return new ExprTestCase("true || true", () => Value.Boolean(true) || Value.Boolean(true), Value.Boolean(true));
            yield return new ExprTestCase("true || false", () => Value.Boolean(true) || Value.Boolean(false), Value.Boolean(true));
            yield return new ExprTestCase("false || true", () => Value.Boolean(false) || Value.Boolean(true), Value.Boolean(true));
            yield return new ExprTestCase("false || false", () => Value.Boolean(false) || Value.Boolean(false), Value.Boolean(false));

            // Int expressions
            yield return new ExprTestCase("1 || 1", () => Value.Number(1) || Value.Number(1), Value.Boolean(true));
            yield return new ExprTestCase("1 || 0", () => Value.Number(1) || Value.Number(0), Value.Boolean(true));
            yield return new ExprTestCase("0 || 1", () => Value.Number(0) || Value.Number(1), Value.Boolean(true));
            yield return new ExprTestCase("0 || 0", () => Value.Number(0) || Value.Number(0), Value.Boolean(false));

            // Bool expressions
            yield return new ExprTestCase("!true", () => !Value.Boolean(true), Value.Boolean(false));
            yield return new ExprTestCase("!false", () => !Value.Boolean(false), Value.Boolean(true));

            // Int expressions
            yield return new ExprTestCase("!1", () => !Value.Number(1), Value.Boolean(false));
            yield return new ExprTestCase("!0", () => !Value.Number(0), Value.Boolean(true));

            // Mixed type expressions
            yield return new ExprTestCase("true && 1", () => Value.Boolean(true) && Value.Number(1), Value.Boolean(true));
            yield return new ExprTestCase("1 && true", () => Value.Number(1) && Value.Boolean(true), Value.Boolean(true));
            yield return new ExprTestCase("true || 1", () => Value.Boolean(true) || Value.Number(1), Value.Boolean(true));
            yield return new ExprTestCase("1 || true", () => Value.Number(1) || Value.Boolean(true), Value.Boolean(true));
            yield return new ExprTestCase("!true", () => !Value.Boolean(true), Value.Boolean(false));
            yield return new ExprTestCase("!1", () => !Value.Number(1), Value.Boolean(false));

            // Complex expressions
            yield return new ExprTestCase("true && (true || false)", () => Value.Boolean(true) && (Value.Boolean(true) || Value.Boolean(false)), Value.Boolean(true));
            yield return new ExprTestCase("true || (true && false)", () => Value.Boolean(true) || (Value.Boolean(true) && Value.Boolean(false)), Value.Boolean(true));
            yield return new ExprTestCase("true && (false || true)", () => Value.Boolean(true) && (Value.Boolean(false) || Value.Boolean(true)), Value.Boolean(true));
            yield return new ExprTestCase("true || (false && true)", () => Value.Boolean(true) || (Value.Boolean(false) && Value.Boolean(true)), Value.Boolean(true));
            yield return new ExprTestCase("true && (true && false)", () => Value.Boolean(true) && (Value.Boolean(true) && Value.Boolean(false)), Value.Boolean(false));
            yield return new ExprTestCase("true || (true || false)", () => Value.Boolean(true) || (Value.Boolean(true) || Value.Boolean(false)), Value.Boolean(true));
            yield return new ExprTestCase("true && (false && true)", () => Value.Boolean(true) && (Value.Boolean(false) && Value.Boolean(true)), Value.Boolean(false));
            yield return new ExprTestCase("true || (false || true)", () => Value.Boolean(true) || (Value.Boolean(false) || Value.Boolean(true)), Value.Boolean(true));
            yield return new ExprTestCase("false && (true || false)", () => Value.Boolean(false) && (Value.Boolean(true) || Value.Boolean(false)), Value.Boolean(false));
        }
    }

    [Test]
    [TestCaseSource(typeof(ExpressionsTest), nameof(ConditionalExpressionsSource))]
    public void ConditionalExpressions(string expr, Func<Value> func, Value expected) {
        var exprResult = ParseExpression(expr).Execute(Ctx).Value;

        var operatorExprResult = func();
        Assert.That(operatorExprResult, Is.EqualTo(exprResult), $"Expected {expr} to be {(expected?.Inspect() ?? "null")} but got {(exprResult?.Inspect() ?? "null")}");

        // Assert.That(exprResult, Is.ValueType(expected));
    }

    [Test]
    public void SimpleExpressionTesting() {
        Value a      = Value.Number(0);
        Value result = a.Operator_ConditionalNot();

        Assert.That(result, Is.Not.Null);
    }


    public static IEnumerable StringOperatorsTestCases {
        get {
            yield return new TestCaseData("'hello' + ' world'", "hello world"); // Concatenation of two strings
            yield return new TestCaseData("'foo' + 'bar'", "foobar");           // Concatenation of two different strings
            yield return new TestCaseData("'hello' + ''", "hello");             // Concatenation with an empty string
            yield return new TestCaseData("'' + 'world'", "world");             // Concatenation of an empty string with a non-empty string
            yield return new TestCaseData("'' + ''", "");                       // Concatenation of two empty strings
            yield return new TestCaseData("'a' + 'b' + 'c'", "abc");            // Multiple concatenations

            // Equality and inequality
            yield return new TestCaseData("'hello' == 'hello'", true);  // Equality comparison (same strings)
            yield return new TestCaseData("'hello' == 'world'", false); // Equality comparison (different strings)
            yield return new TestCaseData("'hello' != 'world'", true);  // Inequality comparison (different strings)
            yield return new TestCaseData("'hello' != 'hello'", false); // Inequality comparison (same strings)

            // Greater than, less than
            yield return new TestCaseData("'apple' < 'banana'", true);   // Lexicographical comparison (less than)
            yield return new TestCaseData("'banana' > 'apple'", true);   // Lexicographical comparison (greater than)
            yield return new TestCaseData("'apple' < 'apple'", false);   // Lexicographical comparison (equal)
            yield return new TestCaseData("'banana' > 'banana'", false); // Lexicographical comparison (equal)
            yield return new TestCaseData("'apple' <= 'banana'", true);  // Lexicographical comparison (less than or equal)
            yield return new TestCaseData("'apple' <= 'apple'", true);   // Lexicographical comparison (equal)
            yield return new TestCaseData("'banana' >= 'apple'", true);  // Lexicographical comparison (greater than or equal)
            yield return new TestCaseData("'banana' >= 'banana'", true); // Lexicographical comparison (equal)

            // Case sensitivity
            yield return new TestCaseData("'Apple' < 'apple'", true); // Case-sensitive comparison (uppercase vs lowercase)
            yield return new TestCaseData("'apple' > 'Apple'", true); // Case-sensitive comparison (lowercase vs uppercase)

            // Edge cases with special characters and whitespace
            yield return new TestCaseData("'hello\nworld' == 'hello\nworld'", true); // Strings with newline characters
            yield return new TestCaseData("'hello\tworld' == 'hello\tworld'", true); // Strings with tab characters
            yield return new TestCaseData("'  ' == '  '", true);                     // Strings with only spaces
            yield return new TestCaseData("'hello ' < 'hello!'", true);              // Special character comparison
            yield return new TestCaseData("'hello ' < 'hello'", false);              // String with space vs. without space
            yield return new TestCaseData("'' == ''", true);                         // Empty strings are equal
            yield return new TestCaseData("' ' == ' '", true);                       // Strings with single space

            // Mixed case
            yield return new TestCaseData("'Hello' + ' ' + 'World'", "Hello World"); // Mixed case concatenation
            yield return new TestCaseData("'HELLO' == 'hello'", false);              // Case-sensitive equality check
            yield return new TestCaseData("'HELLO' != 'hello'", true);               // Case-sensitive inequality check

        }
    }

    [Test]
    [TestCaseSource(typeof(ExpressionsTest), nameof(StringOperatorsTestCases))]
    public void StringOperators(string expression, object expected) {
        var value = ExecuteSimpleExpression(expression);
        Assert.That(value.GetUntypedValue(), Is.EqualTo(expected));
    }

    public static IEnumerable DumbExpressionsTestCases {
        get {
            yield return new TestCaseData("1 == 1", true);
            yield return new TestCaseData("true == true", true);
            yield return new TestCaseData("false == false", true);
            yield return new TestCaseData("0 == false", true);
            yield return new TestCaseData("1 == true", true);
        }
    }

    [Test]
    [TestCaseSource(typeof(ExpressionsTest), nameof(DumbExpressionsTestCases))]
    public void DumbExpressions(string expression, object expected) {
        var value = ExecuteSimpleExpression(expression);
        Assert.That(value.GetUntypedValue(), Is.EqualTo(expected));
    }
}