using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class ArraysTest : IncrementalParserTest
{
    [Test]
    public void LiteralArrayVar() {
        Execute(
            @"
            var arr = [1, true, ""hello"", 'hi', 10];
        "
        );

        var arr = Vars["arr"];
        Assert.That(arr, Is.Not.Null);
    }

    [Test]
    public void Array_Enumerator() {
        Execute(
            """

            var arr = [1, true, "hello", 'hi', 10];
                            
            """
        );

        var arr = Vars["arr"];
        Assert.That(arr.IsEnumerable, Is.True);

        var enumerator = Ctx.Call(arr["getEnumerator"], arr);
        Assert.That(enumerator, Is.Not.Null);
        Assert.That(enumerator["current"], Is.ValueType(Value.Null()));

        var moveNext = Ctx.Call(enumerator["moveNext"], enumerator);
        Assert.That(moveNext, Is.Not.Null);
        Assert.That(moveNext.GetUntypedValue(), Is.EqualTo(true));

        var values = new List<Value> {Value.Int32(1), Value.Boolean(true), Value.String("hello"), Value.String("hi"), Value.Int32(10)};
        enumerator = Ctx.Call(arr["getEnumerator"], arr);
        Assert.That(enumerator.Enumerate(Ctx), Is.EqualTo(values).AsCollection);
    }

    [Test]
    public void LiteralArrayVar_IndexAccess() {
        Execute(
            @"
            var arr = [1, true, ""hello"", 'hi', 10];
            var el0 = arr[0];
            var el1 = arr[1];
            var el2 = arr[2];
            var el3 = arr[3];
            var el4 = arr[4];
        "
        );

        var arr = Vars["arr"];
        Assert.That(arr, Is.Not.Null);

        for (var i = 0; i < 5; i++) {
            var el = Vars[$"el{i}"];
            Assert.That(el, Is.Not.Null);
            Assert.That(el.GetUntypedValue(), Is.EqualTo(arr[i].GetUntypedValue()));
        }

    }

    [Test]
    public void LiteralArrayVar_IndexAccessSetValue() {
        Execute(
            @"
            var arr = [1, true];
            var el0 = arr[0];
            var el1 = arr[1];
            arr[0] = 10;
            arr[1] = false;
            var el2 = arr[0];
            var el3 = arr[1];
        "
        );

        var arr = Vars["arr"];
        Assert.That(arr, Is.Not.Null);

        var el0 = Vars["el0"];
        var el1 = Vars["el1"];
        var el2 = Vars["el2"];
        var el3 = Vars["el3"];

        Assert.Multiple(
            () => {
                Assert.That(el0, Is.Not.Null);
                Assert.That(el1, Is.Not.Null);
                Assert.That(el2, Is.Not.Null);
                Assert.That(el3, Is.Not.Null);
                // Should be the value of arr[0] at the time of assignment, since we're assigning a primitive
                Assert.That(el0.GetUntypedValue(), Is.EqualTo(1));
                Assert.That(el1.GetUntypedValue(), Is.EqualTo(true));
            }
        );

    }

    [Test]
    public void Array_Push() {
        Execute(
            """
            var arr = [];

            var idx = arr.push('hi');
            var newIdx = arr[0];
            var newLen = arr.length;

            inspect(newLen);
            """
        );

        var arr = Vars["arr"];
        Assert.That(arr, Is.Not.Null);

        Assert.That(Variables.RawValue("idx"), Is.EqualTo(1));
        Assert.That(Variables.RawValue("newIdx"), Is.EqualTo("hi"));
        Assert.That(Variables.RawValue("newLen"), Is.EqualTo(1));

        Assert.That(arr.As.Array().Select(e => e.GetUntypedValue()), Is.EquivalentTo(new object[] {"hi"}));


    }
    [Test]
    public void Array_AddingValues() {
        Execute(
            """
            var arr = [];

            var idx = arr.push('hi');
            var newIdx = arr[0];
            var newLen = arr.length;
            inspect(newLen);
            inspect(arr);
            var idx2 = arr.push(1, 2, 3, 4);

            arr.removeAt(0);
            var newLen2 = arr.length;

            arr.removeRange(0, 2);
            var newLen3 = arr.length;
            inspect(newLen);
            inspect(newLen3);
                            
            """
        );

        var arr = Vars["arr"];
        Assert.That(arr, Is.Not.Null);

        Assert.That(Variables.RawValue("idx"), Is.EqualTo(1));
        Assert.That(Variables.RawValue("newIdx"), Is.EqualTo("hi"));
        Assert.That(Variables.RawValue("newLen"), Is.EqualTo(1));
        Assert.That(Variables.RawValue("idx2"), Is.EqualTo(5));

        Assert.That(Variables.RawValue("newLen2"), Is.EqualTo(4));
        Assert.That(Variables.RawValue("newLen3"), Is.EqualTo(2));

        Assert.That(arr.As.Array().Select(e => e.GetUntypedValue()), Is.EquivalentTo(new object[] {3, 4}));


    }
}