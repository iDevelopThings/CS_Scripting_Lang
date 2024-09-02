using System.Collections;
using CSScriptingLang.RuntimeValues;

namespace LanguageTests.InterpreterTests;

[TestFixture]
public class ExpressionsTest : BaseCompilerTest
{
    [Test]
    public void BasicsList() {

        var expressions = new List<(string, object, List<Type>)> {
            ("1.0f + 2.0f", 3.0f, [typeof(float)]),
            ("1.0f - 2.0f", -1.0f, [typeof(float)]),
            ("1.0f * 2.0f", 2.0f, [typeof(float)]),
            ("1.0f / 2.0f", 0.5f, [typeof(float)]),
            ("1.0f % 2.0f", 1.0f, [typeof(float)]),

            ("1.0 + 2.0", 3.0, [typeof(double)]),
            ("1.0 - 2.0", -1.0, [typeof(double)]),
            ("1.0 * 2.0", 2.0, [typeof(double)]),
            ("1.0 / 2.0", 0.5, [typeof(double)]),
            ("1.0 % 2.0", 1.0, [typeof(double)]),

            ("1 + 2", 3, [typeof(int), typeof(double)]),
            ("1 - 2", -1, [typeof(int), typeof(double)]),
            ("1 * 2", 2, [typeof(int), typeof(double)]),
            ("1 / 2", 0, [typeof(int)]),
            ("1 % 2", 1, [typeof(int), typeof(double)]),
        };


        var interp = Execute($"""
                                        
                                                    var results = [{string.Join(",\n", expressions.Select(e => e.Item1))}];
                                                
                                        """);

        var results = interp.Symbols["results"].As<RuntimeValue_Array>();

        for (var i = 0; i < expressions.Count; i++) {
            var expr          = expressions[i];
            var expression    = expr.Item1;
            var expected      = expr.Item2;
            var expectedTypes = expr.Item3;

            var result = results.Get(i);

            if (expected is float or double)
                Assert.That(Convert.ToSingle(expected), Is.EqualTo(Convert.ToSingle(result.Value)));
            else if (expected is int i32)
                Assert.That(i32, Is.EqualTo(result.Value));
            else if (expected is long l)
                Assert.That(l, Is.EqualTo(result.Value));
            else
                Assert.That(expected, Is.EqualTo(result.Value));

            Assert.That(expectedTypes, Does.Contain(result.ValueType));

            // output.WriteLine($"{expression} = {result.Value}(type: {result.ValueType} | {result.RuntimeType} | {result.Type})");
        }

        Assert.That(results.GetField("length").Value, Is.EqualTo(expressions.Count));
    }

    [Test]
    public void ComplexExpressionsList() {
        var complexExpressions = new List<(string, object, List<Type>)> {
            // Mixed type operations
            ("1 + 2.0", 3.0, [typeof(double)]),
            ("1.0f + 2", 3.0f, [typeof(float)]),
            ("1.0 + 2", 3.0, [typeof(double)]),
            ("2.0 + 1", 3.0, [typeof(double)]),
            ("2.0f + 1", 3.0f, [typeof(float)]),
            ("1 + 2.0f", 3.0f, [typeof(float)]),
            ("1 + 2 + 3.0", 6.0, [typeof(double)]),
            ("1 + 2.0 + 3", 6.0, [typeof(double)]),

            // Nested expressions with parentheses
            ("(1 + 2) * 3", 9, [typeof(int)]),
            ("(1.0 + 2.0) * 3.0", 9.0, [typeof(double)]),
            ("1 + (2 * 3)", 7, [typeof(int)]),
            ("(1 + 2) * (3 + 4)", 21, [typeof(int), typeof(double)]),
            ("(1.0 + 2.0) * (3.0 + 4.0)", 21.0, [typeof(double)]),

            // Complex mathematical expressions
            ("(1.0f + 2.0f) / 3.0f", 1.0f, [typeof(float)]),
            ("1 + 2 * 3 - 4 / 2", 5, [typeof(int)]),
            ("1.0 + 2.0 * 3.0 - 4.0 / 2.0", 5.0, [typeof(double)]),

            // Division by zero cases
            ("1 / 0", double.PositiveInfinity, [typeof(double)]), // assuming your logic promotes to double
            ("1.0 / 0.0", double.PositiveInfinity, [typeof(double)]),

            // String concatenation
            ("\"hello\" + \" world\"", "hello world", [typeof(string)]),

            // Edge case: multiple types in one expression
            ("1 + 2.0 + 3.0f", 6.0, [typeof(double)]),
            ("1.0 + 2 + 3.0f", 6.0, [typeof(double)]),

            // Floating point precision
            ("0.1 + 0.2", 0.3, [typeof(double)]), // Be aware of floating-point precision issues

            // Combined arithmetic and function calls
            // ("Math.Sqrt(4) + 2 * 3", Math.Sqrt(4) + 2 * 3, [typeof(double)]),
            // ("Math.Sqrt(9) + Math.Pow(2, 3)", Math.Sqrt(9) + Math.Pow(2, 3), [typeof(double)]),

            // Parentheses and precedence
            ("3 * (2 + 1)", 9, [typeof(int), typeof(double)]),
            ("(3 * 2) + 1", 7, [typeof(int), typeof(double)]),
            ("(3 + 2) * (4 - 1)", 15, [typeof(int), typeof(double)]),
        };

        var interp = Execute($"""
                                        
                                                var results = [{string.Join(",\n", complexExpressions.Select(e => e.Item1))}];
                                            
                                        """);

        var results = interp.Symbols["results"].As<RuntimeValue_Array>();

        for (var i = 0; i < complexExpressions.Count; i++) {
            var expr          = complexExpressions[i];
            var expression    = expr.Item1;
            var expected      = expr.Item2;
            var expectedTypes = expr.Item3;

            var result = results.Get(i);


            if (expected is float or double)
                Assert.That(Convert.ToSingle(expected), Is.EqualTo(Convert.ToSingle(result.Value)));
            else if (expected is int i32)
                Assert.That(i32, Is.EqualTo(result.Value));
            else if (expected is long l)
                Assert.That(l, Is.EqualTo(result.Value));
            else
                Assert.That(expected, Is.EqualTo(result.Value));

            Assert.That(expectedTypes, Does.Contain(result.ValueType));

            // output.WriteLine($"{expression} = {result.Value} ({expected})");
            // output.WriteLine($" -> type: {result.ValueType.ToShortName()} | {result.RuntimeType.GetType().ToShortName()} | {result.Type}");
        }

        Assert.That(results.GetField("length").Value, Is.EqualTo(complexExpressions.Count));
    }

    public static IEnumerable StringOperatorsTestCases {
        get {
            yield return new TestCaseData("\"hello\" + \" world\"", "hello world"); // Concatenation of two strings
            yield return new TestCaseData("\"foo\" + \"bar\"", "foobar");           // Concatenation of two different strings
            yield return new TestCaseData("\"hello\" + \"\"", "hello");             // Concatenation with an empty string
            yield return new TestCaseData("\"\" + \"world\"", "world");             // Concatenation of an empty string with a non-empty string
            yield return new TestCaseData("\"\" + \"\"", "");                       // Concatenation of two empty strings
            yield return new TestCaseData("\"a\" + \"b\" + \"c\"", "abc");          // Multiple concatenations

            // Equality and inequality
            yield return new TestCaseData("\"hello\" == \"hello\"", true);  // Equality comparison (same strings)
            yield return new TestCaseData("\"hello\" == \"world\"", false); // Equality comparison (different strings)
            yield return new TestCaseData("\"hello\" != \"world\"", true);  // Inequality comparison (different strings)
            yield return new TestCaseData("\"hello\" != \"hello\"", false); // Inequality comparison (same strings)

            // Greater than, less than
            yield return new TestCaseData("\"apple\" < \"banana\"", true);   // Lexicographical comparison (less than)
            yield return new TestCaseData("\"banana\" > \"apple\"", true);   // Lexicographical comparison (greater than)
            yield return new TestCaseData("\"apple\" < \"apple\"", false);   // Lexicographical comparison (equal)
            yield return new TestCaseData("\"banana\" > \"banana\"", false); // Lexicographical comparison (equal)
            yield return new TestCaseData("\"apple\" <= \"banana\"", true);  // Lexicographical comparison (less than or equal)
            yield return new TestCaseData("\"apple\" <= \"apple\"", true);   // Lexicographical comparison (equal)
            yield return new TestCaseData("\"banana\" >= \"apple\"", true);  // Lexicographical comparison (greater than or equal)
            yield return new TestCaseData("\"banana\" >= \"banana\"", true); // Lexicographical comparison (equal)

            // Case sensitivity
            yield return new TestCaseData("\"Apple\" < \"apple\"", true); // Case-sensitive comparison (uppercase vs lowercase)
            yield return new TestCaseData("\"apple\" > \"Apple\"", true); // Case-sensitive comparison (lowercase vs uppercase)

            // Edge cases with special characters and whitespace
            yield return new TestCaseData("\"hello\nworld\" == \"hello\nworld\"", true); // Strings with newline characters
            yield return new TestCaseData("\"hello\tworld\" == \"hello\tworld\"", true); // Strings with tab characters
            yield return new TestCaseData("\"  \" == \"  \"", true);                     // Strings with only spaces
            yield return new TestCaseData("\"hello \" < \"hello!\"", true);              // Special character comparison
            yield return new TestCaseData("\"hello \" < \"hello\"", false);              // String with space vs. without space
            yield return new TestCaseData("\"\" == \"\"", true);                         // Empty strings are equal
            yield return new TestCaseData("\" \" == \" \"", true);                       // Strings with single space

            // Mixed case
            yield return new TestCaseData("\"Hello\" + \" \" + \"World\"", "Hello World"); // Mixed case concatenation
            yield return new TestCaseData("\"HELLO\" == \"hello\"", false);                // Case-sensitive equality check
            yield return new TestCaseData("\"HELLO\" != \"hello\"", true);                 // Case-sensitive inequality check

        }
    }

    [Test]
    [TestCaseSource(typeof(ExpressionsTest), nameof(StringOperatorsTestCases))]
    /*
    [InlineData("\"hello\" + \" world\"", "hello world")] // Concatenation of two strings
    [InlineData("\"foo\" + \"bar\"", "foobar")]           // Concatenation of two different strings
    [InlineData("\"hello\" + \"\"", "hello")]             // Concatenation with an empty string
    [InlineData("\"\" + \"world\"", "world")]             // Concatenation of an empty string with a non-empty string
    [InlineData("\"\" + \"\"", "")]                       // Concatenation of two empty strings
    [InlineData("\"a\" + \"b\" + \"c\"", "abc")]          // Multiple concatenations

    // Equality and inequality
    [InlineData("\"hello\" == \"hello\"", true)]  // Equality comparison (same strings)
    [InlineData("\"hello\" == \"world\"", false)] // Equality comparison (different strings)
    [InlineData("\"hello\" != \"world\"", true)]  // Inequality comparison (different strings)
    [InlineData("\"hello\" != \"hello\"", false)] // Inequality comparison (same strings)

    // Greater than, less than
    [InlineData("\"apple\" < \"banana\"", true)]   // Lexicographical comparison (less than)
    [InlineData("\"banana\" > \"apple\"", true)]   // Lexicographical comparison (greater than)
    [InlineData("\"apple\" < \"apple\"", false)]   // Lexicographical comparison (equal)
    [InlineData("\"banana\" > \"banana\"", false)] // Lexicographical comparison (equal)
    [InlineData("\"apple\" <= \"banana\"", true)]  // Lexicographical comparison (less than or equal)
    [InlineData("\"apple\" <= \"apple\"", true)]   // Lexicographical comparison (equal)
    [InlineData("\"banana\" >= \"apple\"", true)]  // Lexicographical comparison (greater than or equal)
    [InlineData("\"banana\" >= \"banana\"", true)] // Lexicographical comparison (equal)

    // Case sensitivity
    [InlineData("\"Apple\" < \"apple\"", true)] // Case-sensitive comparison (uppercase vs lowercase)
    [InlineData("\"apple\" > \"Apple\"", true)] // Case-sensitive comparison (lowercase vs uppercase)

    // Edge cases with special characters and whitespace
    [InlineData("\"hello\nworld\" == \"hello\nworld\"", true)] // Strings with newline characters
    [InlineData("\"hello\tworld\" == \"hello\tworld\"", true)] // Strings with tab characters
    [InlineData("\"  \" == \"  \"", true)]                     // Strings with only spaces
    [InlineData("\"hello \" < \"hello!\"", true)]              // Special character comparison
    [InlineData("\"hello \" < \"hello\"", false)]              // String with space vs. without space
    [InlineData("\"\" == \"\"", true)]                         // Empty strings are equal
    [InlineData("\" \" == \" \"", true)]                       // Strings with single space

    // Mixed case
    [InlineData("\"Hello\" + \" \" + \"World\"", "Hello World")] // Mixed case concatenation
    [InlineData("\"HELLO\" == \"hello\"", false)]                // Case-sensitive equality check
    [InlineData("\"HELLO\" != \"hello\"", true)]                 // Case-sensitive inequality check




    */
    public void StringOperators(string expression, object expected) {
        var (interp, result) = ExecuteSingleExpression(expression);

        Assert.That(expected, Is.EqualTo(result.RawValue));
    }
}