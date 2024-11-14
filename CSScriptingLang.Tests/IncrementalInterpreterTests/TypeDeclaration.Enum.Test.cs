using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class TypeDeclaration_Enum_Test : IncrementalParserTest
{
    [Test]
    public void Declaration_Basic() {
        Execute(
            """
            type MyEnum enum {
                One
                Two
                Three
            }
            var one = MyEnum.One;
            var two = MyEnum.Two;
            inspect(one);
            inspect(two);
            inspect(MyEnum);
            """
        );

        var one = Vars["one"];
        var two = Vars["two"];

        one.Should().NotBeNull();
        two.Should().NotBeNull();

    }
    [Test]
    public void Declaration_WithValues() {
        Execute(
            """
            type MyEnum enum {
                None = 0
                A = 2
                B = 4
                C = 8
            }
            inspect(MyEnum);
            inspect(MyEnum.A + MyEnum.B);
            """
        );

        var enumValue = Vars["MyEnum"];
        enumValue.Should().NotBeNull();
        
        enumValue["None"]["value"].Should().MatchRawValue(0);
        enumValue["A"]["value"].Should().MatchRawValue(2);
        enumValue["B"]["value"].Should().MatchRawValue(4);
        enumValue["C"]["value"].Should().MatchRawValue(8);

    }
    [Test]
    public void Declaration_WithCtorValues() {
        Execute(
            """
            type MyEnum enum {
                None(-1, null)
                A(0, {objName: "A"})
                B(1, {objName: "B"})
                C(2, {objName: "C"})
                
                MyEnum(int v, obj b);
            }
            inspect(MyEnum.A);
            inspect(MyEnum);
            """
        );

        var enumValue = Vars["MyEnum"];
        enumValue.Should().NotBeNull();

        enumValue["A"]["v"].Should().MatchRawValue(0);
        enumValue["A"]["b"]["objName"].Should().MatchRawValue("A");

        enumValue["B"]["v"].Should().MatchRawValue(1);
        enumValue["B"]["b"]["objName"].Should().MatchRawValue("B");
    }
}