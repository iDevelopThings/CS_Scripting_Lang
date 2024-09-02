using CSScriptingLang.RuntimeValues;

namespace LanguageTests.InterpreterTests;

public class ArraysTest : BaseCompilerTest
{
    [Test]
    public void LiteralArrayVar() {
        var interp = Execute(@"
            var arr = [1, true, ""hello"", 'hi', 10];
        ");

        var arr = interp.Symbols.Get<RuntimeValue_Array>("arr");
        Assert.That(arr, Is.Not.Null);
    }

    [Test]
    public void LiteralArrayVar_IndexAccess() {
        var interp = Execute(@"
            var arr = [1, true, ""hello"", 'hi', 10];
            var el0 = arr[0];
            var el1 = arr[1];
            var el2 = arr[2];
            var el3 = arr[3];
            var el4 = arr[4];
        ");

        var arr = interp.Symbols.Get<RuntimeValue_Array>("arr");
        Assert.That(arr, Is.Not.Null);

        for (var i = 0; i < 5; i++) {
            var el = interp.Symbols.Get<RuntimeValue>($"el{i}");
            Assert.That(el, Is.Not.Null);
            Assert.That(el.Value, Is.EqualTo(arr[i].Value));
        }

    }

    [Test]
    public void LiteralArrayVar_IndexAccessSetValue() {
        var interp = Execute(@"
            var arr = [1, true];
            var el0 = arr[0];
            var el1 = arr[1];
            arr[0] = 10;
            arr[1] = false;
            var el2 = arr[0];
            var el3 = arr[1];
        ");

        var arr = interp.Symbols.Get<RuntimeValue_Array>("arr");
        Assert.That(arr, Is.Not.Null);

        var el0 = interp.Symbols.Get<RuntimeValue>("el0");
        var el1 = interp.Symbols.Get<RuntimeValue>("el1");
        var el2 = interp.Symbols.Get<RuntimeValue>("el2");
        var el3 = interp.Symbols.Get<RuntimeValue>("el3");

        Assert.Multiple(() => {
            Assert.That(el0, Is.Not.Null);
            Assert.That(el1, Is.Not.Null);
            Assert.That(el2, Is.Not.Null);
            Assert.That(el3, Is.Not.Null);
            Assert.That(el0.Value, Is.EqualTo((double) 10));
            Assert.That(el1.Value, Is.EqualTo(false));
        });

    }
}