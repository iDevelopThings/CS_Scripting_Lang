namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class SwitchMatchTest : IncrementalParserTest
{
    [Test]
    public void Simple() {
        Execute(
            """
            function testValuePrint(object value) {
                match(value) {
                    case 1      => print('input: {0} hit `case 1`', value)
                    case true   => print('input: {0} hit `case true`', value)
                    case false  => print('input: {0} hit `case false`', value)
                    case is i32 => print('input: {0} hit `case is i32`', value)
                    case 'hi'   => print('input: {0} hit `case hi`', value)
                    case is str {
                        print('input: {0} hit `case is str`', value)
                    }
                    case _      => print('input: {0} hit `case _`', value)
                }
            }

            var tests = [0, 1, true, false, 'hi', 'bye', 'try'];
            
            for(var (key, value) = range tests) {
                testValuePrint(value);
                //var testResult = testValue(key);
                //print('test: {0}, result: {1}, expected: {2}. Did Match == {3}', key, testResult, expected, testResult == expected);
            }
            
            """
        );
    }
}