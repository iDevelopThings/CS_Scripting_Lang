using System.Collections;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Values;

namespace LanguageTests.InterpreterTests;

[TestFixture]
public class SwitchMatchTest : BaseCompilerTest
{
    [Test]
    public void Simple() {
        Execute(
            """
          
            function testValue(object value) {
                return match(value) {
                    case 1 => 2
                    case true => 4
                    case is i32 => 1
                    case 'hi' => 3
                    case _ => 5
                }
            }

            var tests = {
                0 : 5,
                1 : 2,
                true : 4,
                false : 5,
                'hi' : 3,
                'bye' : 5,
            };
            
            for(var (key, expected) = range tests) {
                var testResult = testValue(key);
                print('test: {0}, result: {1}, expected: {2}. Did Match == {3}', key, testResult, expected, testResult == expected);
            }
            
            """
        );
    }
}