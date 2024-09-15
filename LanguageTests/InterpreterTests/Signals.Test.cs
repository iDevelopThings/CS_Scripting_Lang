using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Values;

namespace LanguageTests.InterpreterTests;

[TestFixture]
public class SignalsTest : BaseCompilerTest
{
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

        var signal = Variables.Get<ValueSignal>("some_signal");

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

        var signal = Variables.Get<ValueSignal>("some_signal");

        Assert.That(signal, Is.Not.Null);
    }

    [Test]
    public void UnSubscribingWithFunctionDeclaration() {
        Execute(
            """
            signal some_signal(int x);

            var calls = 0;

            some_signal += function(int x) {
                calls++;
                print('Received signal: {0} - Calls: {1}', x, calls);
            };

            some_signal.emit(1);
            
            inspect(some_signal);

            some_signal -= function(int x) {
                calls++;
                print('Received signal: {0} - Calls: {1}', x, calls);
            };

            some_signal.emit(2);
            
            inspect(some_signal);

            """
        );

        var signal = Variables.Get<ValueSignal>("some_signal");

        Assert.That(signal, Is.Not.Null);

        Assert.That(Variables.Get<Number_Int32>("calls").Value, Is.EqualTo(1));
    }

    [Test]
    public void UnSubscribingWithFunctionRef() {
        Execute(
            """
            
            signal some_signal(int x);
            
            var calls = 0;
            
            some_signal += signalListener;
            
            some_signal.emit(1);
            
            inspect(some_signal);
            
            some_signal -= signalListener;
            
            some_signal.emit(2);
            
            inspect(some_signal);

            function signalListener(int x) {
                calls++;
                print('Received signal: {0} - Calls: {1}', x, calls);
            };

            """
        );

        var signal = Variables.Get<ValueSignal>("some_signal");

        Assert.That(signal, Is.Not.Null);
        Assert.That(Variables.Get<Number_Int32>("calls").Value, Is.EqualTo(1));
    }

    [Test]
    public void Emitting() {
        Execute(
            """
            signal some_signal(int a, int b);

            some_signal += function(int a, int b) {
                print('Received signal: {0}, {1}', a, b);
            };

            some_signal.emit(1, 2);

            """
        );

        var signal = Variables.Get<ValueSignal>("some_signal");

        Assert.That(signal, Is.Not.Null);
    }
}