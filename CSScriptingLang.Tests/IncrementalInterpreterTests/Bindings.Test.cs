using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[LanguageClassBind("TestObj")]
public partial class TestingObject : Value
{
    [LanguageValueConstructor]
    public TestingObject() {
        Console.WriteLine("Hello from TestingObject");
    }
    [LanguageValueConstructor]
    public TestingObject(bool ctorValue) {
        Console.WriteLine("Hello from TestingObject = " + ctorValue);
    }

    [LanguageFunction]
    public string SomeStr { get; set; }

    [LanguageFunction]
    public int SomeInt { get; set; }

    [LanguageFunction]
    public void SomeMethod() {
        Console.WriteLine("Hello from SomeMethod");
    }

    [LanguageFunction]
    public void SomeMethodWithArgs(int a, int b) {
        Console.WriteLine("Hello from SomeMethodWithArgs = " + (a + b));
    }

    [LanguageFunction]
    public string SomeMethodWithReturn() {
        return "Hello from SomeMethodWithReturn";
    }
    
    [LanguageOperator("+")]
    public static int operator_add(Value a, Value b) {
        return b + 1;
    }
}

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class BindingsTest : IncrementalParserTest
{
    [Test]
    public void Module_GlobalFunctions() {
        Execute(
            """
            inspect(true);
            print('hi');
            """
        );

        Assert.Multiple(() => {
            Assert.That(Ctx.AllVariables.ContainsKey("inspect"), Is.True);
            Assert.That(Ctx.AllVariables.ContainsKey("print"), Is.True);
        });
    }

    [Test]
    public void Object() {
        
        Ctx.OnPreLoadLibraries += lib => {
            lib.Add(new TestingObject.Library());
        };

        Execute(
            """
            var myObj = new<TestObj>();
            inspect(myObj);
            """
        );

        var obj = Ctx.GetVariable("myObj");
        Assert.That(obj, Is.Not.Null);
    }
    
    [Test]
    public void Object_OperatorOverloads() {
        Ctx.OnPreLoadLibraries += lib => {
            lib.Add(new TestingObject.Library());
        };

        Execute(
            """
            
            var obj = {
                operator_add: function(object a, int b) {
                    inspect(a);
                    return b + 1;
                }
            };
            
            // var myObj = new<TestObj>();
            var resultA = obj + 1;
            var resultB = obj + 2;
            
            inspect(obj);
            inspect(resultA);
            inspect(resultB);
            
            // 
            // var pls = obj + 1;
            

            """
        );

        var obj = Ctx.GetVariable("obj");
        Assert.That(obj, Is.Not.Null);
        
        Assert.That(Vars["resultA"].As.Int32(), Is.EqualTo(2));
        Assert.That(Vars["resultB"].As.Int32(), Is.EqualTo(3));
        // Assert.That(Vars["resultC"].As.Int32(), Is.EqualTo(1));
        // Assert.That(Vars["resultD"].As.Int32(), Is.EqualTo(2));
    }
}