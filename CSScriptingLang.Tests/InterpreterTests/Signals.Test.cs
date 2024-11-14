using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests.InterpreterTests;

[TestFixture]
public class SignalsTest : VirtualFS_CompilerTest
{
    [Test]
    public void GlobalSignal() {
        Execute(
            """
            signal some_signal(int a, int b);
            inspect(some_signal);
            """
        );

        var signal = Vars["some_signal"];

        Assert.That(signal, Is.Not.Null);
    }

    [Test]
    public void Parsing() {
        Execute(
            """

            function nested() {
                signal nested_signal(int a, int b);
            }

            signal some_signal(int a, int b);

            """
        );

        var signalVar = Vars["some_signal"];
        Assert.That(signalVar, Is.Not.Null);

        var signal = signalVar.As.Signal();
        Assert.That(signal, Is.Not.Null);

    }

    [Test]
    public void Subscribing() {
        Execute(
            """
            signal some_signal(int a, int b);

            some_signal += function(int a, int b) {
                print('Received signal: {0}, {1}', a, b);
            };

            """
        );

        var signalVar = Vars["some_signal"];
        Assert.That(signalVar, Is.Not.Null);

        var signal = signalVar.As.Signal();
        Assert.That(signal, Is.Not.Null);

        Assert.That(signal.Listeners.Count, Is.EqualTo(1));

    }

    [Test]
    public void Unsubscribing() {
        Execute(
            """
            signal some_signal(int x);

            var listener = () => print('Received signal: {0}', x);

            some_signal += listener;

            inspect(some_signal);

            some_signal -= listener;

            inspect(some_signal);
            
            some_signal.emit(1);
            some_signal.emit(2);
            some_signal.emit(3);

            """
        );

        var signalVar = Vars["some_signal"];
        Assert.That(signalVar, Is.Not.Null);

        var signal = signalVar.As.Signal();
        Assert.That(signal, Is.Not.Null);

        Assert.That(signal.Listeners.Count, Is.EqualTo(0));
    }

    [Test]
    public void UnSubscribing_DifferentFnTypes() {
        Execute(
            """
            signal some_signal(int x);

            var listener = () => print('Received signal: {0}', x);
            function listener2(int x) {
                print('Received signal: {0}', x);
            }
            var listener3 = function(int x) {
                print('Received signal: {0}', x);
            };

            some_signal += listener;
            some_signal += listener2;
            some_signal += listener3;
            // some_signal += function(int x) { print('is it same?'); };

            inspect(some_signal);

            some_signal -= listener;
            some_signal -= listener2;
            some_signal -= listener3;
            // some_signal -= function(int x) { print('is it same?'); };
            
            inspect(some_signal);

            """
        );

        var signalVar = Vars["some_signal"];
        Assert.That(signalVar, Is.Not.Null);

        var signal = signalVar.As.Signal();
        Assert.That(signal, Is.Not.Null);

        Assert.That(signal.Listeners.Count, Is.EqualTo(0));
    }


    [Test]
    public void Emitting() {
        Execute(
            """
            signal some_signal(int a, int b);
            
            var calls = 0;
            
            some_signal += function(int a, int b) {
                print('Received signal: {0}, {1}', a, b);
                calls += 1;
            };

            some_signal.emit(1, 2);

            """
        );

        var signalVar = Vars["some_signal"];
        Assert.That(signalVar, Is.Not.Null);

        var signal = signalVar.As.Signal();
        Assert.That(signal, Is.Not.Null);
        
        Assert.That(signal.Listeners.Count, Is.EqualTo(1));
        Assert.That(Vars["calls"].As.Int(), Is.EqualTo(1));

    }
}