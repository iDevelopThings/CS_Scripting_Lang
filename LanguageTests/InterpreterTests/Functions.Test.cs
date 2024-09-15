using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Values;

namespace LanguageTests.InterpreterTests;

[TestFixture]
public class FunctionsTest : BaseCompilerTest
{
    [Test]
    public void FunctionDeclaration() {
        Execute(@"
            function add(int a, int b) {
                return a + b;
            }
        ");

        var doesExist = Variables.Get("add", out var add);
        Assert.That(doesExist);
        Assert.That(add.Val, Is.Not.Null);
        Assert.That(add.Val.As.Fn(), Is.Not.Null.And.InstanceOf<FnClosure>());
    }

    [Test]
    public void FunctionExecution_ReturnValueVarStorage() {
        Execute(@"
            function add(int a, int b) {
                return a + b;
            }
            var result = add(10, 20);
        ");

        Assert.That(Variables["result"].Val.As.Int(), Is.EqualTo(30));
    }

    [Test]
    public void FunctionExecution_Void() {
        Execute(@"
            function add(int a, int b) {
                print(a + b);
            }
            add(10, 20);
            var result = add(1, 2);
        ");

        var result = Variables["result"];
        var value  = result.RawValue;

        Assert.That(result.Val.Is.Null, Is.True);
    }

    [Test]
    public void FunctionExecution_Nested() {
        Execute(@"
            function add(int a, int b) {
                function sub(int c, int d) {
                    return c - d;
                }
                return a + b + sub(a, b);
            }
            var result = add(10, 20);
        ");

        Assert.That(Variables["result"].RawValue, Is.EqualTo(20));
    }

    [Test]
    public void FunctionExecution_Nested_InheritingParentScope() {
        TrackFunctionFrames();

        Execute(@"
            function add(int a, int b) {
                function sub(int c) {
                    return a - c;
                }
                return a + b + sub(b);
            }
            var result = add(10, 20);
        ");

        // Assert.That(GetPushedFrame("add").Context.SymbolTable.GetFunctionDeclaration("add"), Is.Not.Null);
        Assert.That(Variables.Get("a"), Is.Null);
        Assert.That(Variables.Get("b"), Is.Null);
        Assert.That(Variables.Get("c"), Is.Null);
        Assert.That(Variables.Get("result"), Is.Not.Null);

        Assert.That(Variables["result"].Val.As.Int(), Is.EqualTo(20));
    }

    [Test]
    public void FunctionTypes_Equality() {

        Execute(@"
            var a = function(int a, int b) { print('hi'); }
            var b = function(int a, int b) { print('bye'); }

            var eq = a == b; // false?
            var aEq = a == a; // true?
            var bEq = b == b; // true?
            
            function add(int a, int b) { print('hi'); }
            function sub(int a, int b) { print('bye'); }

            var addEq = add == sub; // false?
            var addEq2 = add == add; // true?

            var aCopy = a;
            var aEqCopy = a == aCopy; // true?
            
        ");

        var a  = Variables.Get("a");
        var b  = Variables.Get("b");
        var eq = Variables.Get("eq");

        var aEq    = Variables.Get("aEq");
        var bEq    = Variables.Get("bEq");
        var addEq  = Variables.Get("addEq");
        var addEq2 = Variables.Get("addEq2");


        Assert.That(a.RawValue, Is.Not.EqualTo(b.RawValue));
        Assert.That(eq.RawValue, Is.False);

        Assert.That(aEq.RawValue, Is.True);
        Assert.That(bEq.RawValue, Is.True);
        Assert.That(addEq.RawValue, Is.False);
        Assert.That(addEq2.RawValue, Is.True);
        Assert.That(Variables.Get("aCopy").RawValue, Is.EqualTo(a.RawValue));
    }

    [Test]
    public void LambdaFunction() {
        Execute(
            """
            var a = (int a, int b) => { print('hi'); };
            inspect(a);
            a(0,0);
                              
            var (x, y) = (10, 20);
            inspect(x);
            inspect(y);
             
            var z = (1 + 2) * 3;
            inspect(z);

            """
        );

        var a = Variables.Get("a");

        Assert.That(a.RawValue, Is.Not.Null);
        Assert.That(a.Val, Is.TypeOf<Value>());
    }
}