using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace LanguageTests.Core;

[TestFixture]
public class ValueAndTypeTests : BaseCompilerTest
{
    [Test]
    public void NativeMethodBindings() {

        var ctx = new ExecContext(Interpreter);

        var boundStatics = NativeBinder.BindNativeFunctions(ctx);

        var obj      = new ValueSignal();
        var bindings = obj.LoadNativeMethodBindings();

        Assert.That(bindings, Is.Not.Empty);
        Assert.That(bindings.Any(b => b.Name == "emit"), Is.True);
    }

    [Test]
    public void NativeFieldBindings() {

        var ctx = new ExecContext(Interpreter);
        NativeBinder.BindNativeFunctions(ctx);

        var arr = ValueFactory.Array.Make();

        arr.Add(ValueFactory.Make(10));
        arr.Add(ValueFactory.Make(20));

        var bindings = arr.LoadNativeFieldBindings();


        Assert.That(bindings, Is.Not.Empty);
        Assert.That(bindings.Any(b => b.Key == "length"), Is.True);
        Assert.That(arr.HasField("length"), Is.True);

        var len = arr.GetField("length");
        Assert.That(len, Is.Not.Null);
        Assert.That(len.Value<int>(), Is.EqualTo(2));

        arr.SetField("length", ValueFactory.Make(10));


    }
}