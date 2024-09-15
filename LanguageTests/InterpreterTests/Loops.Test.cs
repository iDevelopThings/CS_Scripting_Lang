namespace LanguageTests.InterpreterTests;

[TestFixture]
public class LoopsTest : BaseCompilerTest
{
    [Test(Description = "For i; i < 10; i = i + 1")]
    public void For_i_plus_1() {
        Execute(
            """
            var j = 0;
            for (var i = 0; i < 10; i = i + 1) {
                print(i);
                j = i;
            }
                    
            """
        );

        Assert.That(Variables["j"].RawValue, Is.EqualTo(9));
    }

    [Test(Description = "For i; i < 10; i++")]
    public void For_i_plus_plus() {
        Execute(
            """
            var j = 0;
            for (var i = 0; i < 10; i++) {
                print(i);
                j = i;
            }
            """
        );

        Assert.That(Variables["j"].RawValue, Is.EqualTo(10));
    }

    [Test(Description = "Reverse For i; i > 0; i--")]
    public void For_i_minus_minus() {
        Execute(
            """

            var j = 0;
            for (var i = 10; i > 0; i--) {
                j = i;
                print(i);
            }
                    
            """
        );

        Assert.That(Variables?["j"]?.RawValue, Is.EqualTo(0));
    }

    [Test(Description = "For using a variable from the outer scope")]
    public void For_outer_scope_variable() {
        Execute(
            """

            var i = 0;
            for (; i < 10; i++) {
                print(i);
            }
                    
            """
        );

        Assert.That(Variables["i"].RawValue, Is.EqualTo(10));
    }

    [Test]
    public void ForRange() {
        Execute(
            """

            var counter = 0;
            for (var i = range 10) {
                print(i);
                counter = i;
            }
                    
            """
        );

        Assert.That(Variables["counter"].RawValue, Is.EqualTo(10));
    }

    [Test]
    public void ForRange_Tuple_IndexValue() {
        Execute(
            """
            
                        var counter = 0;
                        for (var (i, el) = range 10) {
                            print(i);
                            counter = i;
                        }
                    
            """
        );

        Assert.That(Variables["counter"].RawValue, Is.EqualTo(10));
    }

    [Test]
    public void ForRange_Tuple_Index() {
        // Index is the same as the value
        Execute(
            """

            var counter = 0;
            for (var (i) = range 10) {
                print(i);
                counter = i;
            }
                    
            """
        );

        Assert.That(Variables["counter"].RawValue, Is.EqualTo(10));
    }

    [Test]
    public void ForRange_Array() {
        Execute(
            """
            var j = 0;
            var jEl = 0;

            var arr = [1, 2, 3, 4, 5];
            for (var (i, el) = range arr) {
                print(el);
                j = i;
                jEl = el;
            }
                    
            """
        );

        Assert.That(Variables["jEl"].RawValue, Is.EqualTo(5));
    }
    [Test]
    public void ForRange_Object() {
        Execute(
            """
            var j = 0;
            var jEl = 0;

            var obj = {
                a: 1,
                b: 2,
                c: 3,
            };
            for (var (i, el) = range obj) {
                print('key: {0}, value: {1}', i, el);
                j = i;
                jEl = el;
            }
                    
            """
        );

        Assert.That(Variables["j"].RawValue, Is.EqualTo("c"));
        Assert.That(Variables["jEl"].RawValue, Is.EqualTo(3));
    }
    [Test]
    public void ForRange_String() {
        Execute(
            """
            var j = 0;
            var jEl = 0;

            for (var (i, el) = range "abcdef") {
                print('key: {0}, value: {1}', i, el);
                j = i;
                jEl = el;
            }
                    
            """
        );

        Assert.That(Variables["j"].RawValue, Is.EqualTo(5));
        Assert.That(Variables["jEl"].RawValue, Is.EqualTo("f"));
    }
}