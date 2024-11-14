using System.Collections;

namespace CSScriptingLang.Tests.InterpreterTests;

[TestFixture]
public class SyntaxBasicsTest : VirtualFS_CompilerTest
{
    [Test]
    public void LAndRValue_Variables() {
        Execute(
            """
            var a = 1;
            var b = 2;
            a = b;  
            """
        );

        Assert.Multiple(() => {
            Assert.That(Variables["a"].RawValue, Is.EqualTo((double) 2));
            Assert.That(Variables["b"].RawValue, Is.EqualTo((double) 2));
        });
    }

    [Test]
    public void LAndRValue_ObjectMembers() {
        Execute(
            """
            var obj  = { a: 1, b: 2, c: 3 };
            obj.a = obj.b;
            obj.c = 0;

            """
        );

        var obj = Variables["obj"].Val;
        Assert.Multiple(() => {
            Assert.That(obj.GetMember("a").GetUntypedValue(), Is.EqualTo(2));
            Assert.That(obj.GetMember("b").GetUntypedValue(), Is.EqualTo(2));
            Assert.That(obj.GetMember("c").GetUntypedValue(), Is.EqualTo(0));
        });
    }

    [Test]
    public void VarTypes_Int() {
        Execute("""
                var a = 10;
                """);
        Assert.That(Variables["a"].RawValue, Is.EqualTo((double) 10));
    }
    [Test]
    public void VarTypes_Bool() {
        Execute("""
                var a = true;
                var b = false;
                """);
        Assert.That(Variables["a"].RawValue, Is.EqualTo(true));
        Assert.That(Variables["b"].RawValue, Is.EqualTo(false));
    }

    [Test]
    public void VarTypes_String() {
        Execute("""
                var a = "Hello World";
                var b = 'Hello World';
                """);
        Assert.That(Variables["a"].RawValue, Is.EqualTo("Hello World"));
        Assert.That(Variables["b"].RawValue, Is.EqualTo("Hello World"));
    }
    [Test]
    public void VarTypes_AssigningShit() {
        Execute(
            """
            var a = 'hello';
            a.pls = () => {
                print('dyn func');
            }; 
            a.pls();
            """
        );
        var a = Variables["a"];
        Assert.That(a.RawValue, Is.EqualTo("hello"));
        Assert.That(a.Val.GetMember("pls"), Is.Not.Null);
    }

    public static IEnumerable IfStatementsTestCases {
        get {
            yield return new TestCaseData(2, 0, 1);
            yield return new TestCaseData(0, 2, 0);
        }
    }

    [Test]
    [TestCaseSource(typeof(SyntaxBasicsTest), nameof(IfStatementsTestCases))]
    public void IfStatements(double a, double b, double res) {
        Execute(
            $$$"""        
               var a = {{{a}}};
               var b = {{{b}}};
               var res = 0;

               if (a > b) {
                   res = 1;
               }
               print('a: {0}, b: {1}, res: {2}', a, b, res == {{{res}}});

               """
        );

        Assert.Multiple(() => {
            Assert.That(Variables["a"].RawValue, Is.EqualTo(a));
            Assert.That(Variables["b"].RawValue, Is.EqualTo(b));
            Assert.That(Variables["res"].RawValue, Is.EqualTo(res));
        });
    }

    [Theory]
    [TestCaseSource(typeof(SyntaxBasicsTest), nameof(IfStatementsTestCases))]
    public void IfElseStatements(double a, double b, double res) {
        Execute(
            $$$"""
               var a = {{{a}}};
               var b = {{{b}}};
               var res = 0;

               if (a > b) {
                   res = 1;
               } else {
                   res = 0;
               }
               print('a: {0}, b: {1}, res: {2}', a, b, res == {{{res}}});
               """
        );

        Assert.Multiple(() => {
            Assert.That(Variables["a"].RawValue, Is.EqualTo(a));
            Assert.That(Variables["b"].RawValue, Is.EqualTo(b));
            Assert.That(Variables["res"].RawValue, Is.EqualTo(res));
        });
    }
}