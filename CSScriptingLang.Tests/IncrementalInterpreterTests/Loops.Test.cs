namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class LoopsTest : IncrementalParserTest
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
        Vars["j"].Should().MatchRawValue(0);
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
        Vars["i"].Should().MatchRawValue(10);
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
        Vars["counter"].Should().MatchRawValue(9);
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
        Vars["counter"].Should().MatchRawValue(9);
    }

    [Test]
    public void ForRange_Tuple_Index() {
        Execute(
            """
            var counter = 0;
            for (var i = range 10) {
                print(i);
                counter = i;
            }
            """
        );
        Vars["counter"].Should().MatchRawValue(9);
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
        Vars["jEl"].Should().MatchRawValue(5);
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
        Vars["j"].Should().MatchRawValue("c");
        Vars["jEl"].Should().MatchRawValue(3);
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

        Vars["j"].Should().MatchRawValue(6);
        Vars["jEl"].Should().MatchRawValue("f");
    }
    [Test]
    public void ForBreak() {
        Execute(
            """
            var j = 0;
            for (var i = 0; i < 10; i++) {
                j = i;
                if(i == 4) {
                    print('continue');
                    continue;
                }
                print(i);
                if (i == 5) {
                    print('break');
                    break;
                }
            }
            """
        );
        Vars["j"].Should().MatchRawValue(5);
    }
    [Test]
    public void RegularForWhileLoop() {
        Execute(
            """
            var j = 0;
            for {
                j = j + 1;
                print(j);
                if (j == 5) {
                    break;
                }
            }
            """
        );
        Vars["j"].Should().MatchRawValue(5);
    }
}