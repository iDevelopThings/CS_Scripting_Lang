using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace LanguageTests.InterpreterTests;

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
    public int OperatorPlus(int b) {
        return 1;
    }
}

public class BindingsTest : BaseCompilerTest
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
            var myObj = new<TestObj>();
            var result = myObj + 1;
            inspect(myObj);

            """
        );

        var obj = Ctx.GetVariable("myObj");
        Assert.That(obj, Is.Not.Null);
    }
}