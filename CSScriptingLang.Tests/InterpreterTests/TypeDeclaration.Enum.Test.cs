using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests.InterpreterTests;

[TestFixture]
public class TypeDeclaration_Enum_Test : VirtualFS_CompilerTest
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
            // inspect(one);
            // inspect(two);
            inspect(MyEnum);
            """
        );

        var one = Vars["one"];
        var two = Vars["two"];
        
        Assert.That(one, Is.Not.Null);
        Assert.That(two, Is.Not.Null);
        
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
        enumValue["A"].ToDebugString();
        
        Assert.That(enumValue, Is.Not.Null);
        
    }
    [Test]
    public void Declaration_WithCtorValues() {
        Execute(
            """
            type MyEnum enum {
                None(-1, null)
                A(0, {name: "A"})
                B(1, {name: "B"})
                C(2, {name: "C"})
                
                MyEnum(int v, obj b);
            }
            inspect(MyEnum.A);
            inspect(MyEnum);
            """
        );

        var enumValue = Vars["MyEnum"];
        Assert.That(enumValue, Is.Not.Null);
        
        Assert.That(enumValue["A"]["v"], Is.ValueType(Value.Int32(0)));
        Assert.That(enumValue["A"]["b"]["name"], Is.ValueType(Value.String("A")));
        
        Assert.That(enumValue["B"]["v"], Is.ValueType(Value.Int32(1)));
        Assert.That(enumValue["B"]["b"]["name"], Is.ValueType(Value.String("B")));
    }
}