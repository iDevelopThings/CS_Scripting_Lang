using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

using CSScriptingLang.Tests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class SignalsTest : IncrementalParserTest
{
    [Test]
    public void SignalDeclaration_Global() {
        Execute(
            """
            signal some_signal(int a, int b);
            inspect(some_signal);
            """
        );

        var signal = Vars["some_signal"];
        signal.Should().NotBeNull()
           .And
           .Property(x => x.Subject.As.Signal())
           .ShouldEventually().NotBeNull();
    }

    [Test]
    public void SignalDeclaration_Nested() {
        Execute(
            """
            function nested() {
                signal nested_signal(int a, int b);
            }

            signal some_signal(int a, int b);
            """
        );

        var signal = Vars["some_signal"];

        signal.Should().NotBeNull()
           .And
           .Property(x => x.Subject.As.Signal())
           .ShouldEventually().NotBeNull();


        var nestedSignal = Vars["nested_signal"];

        nestedSignal.Should().BeNull();
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

        signalVar.Should()
           .NotBeNull()
           .And.Match(x => x.As.Signal() != null)
           .And.Match(x => x.As.Signal().Listeners.Count == 1);

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

        var signal = Vars["some_signal"];

        signal.Should()
           .NotBeNull()
           .And.Match(x => x.As.Signal() != null)
           .And.Match(x => x.As.Signal().Listeners.Count == 0);
        
        // signal.As.Signal().Should()
           // .EmitValues(Value.Number(1), Value.Number(2), Value.Number(3));
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

        var signal = Vars["some_signal"];
        
        signal.Should()
           .NotBeNull()
           .And.Match(x => x.As.Signal() != null)
           .And.Match(x => x.As.Signal().Listeners.Count == 0);
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

        var signal = Vars["some_signal"];
        signal.Should()
           .NotBeNull()
           .And.Match(x => x.As.Signal() != null)
           .And.Match(x => x.As.Signal().Listeners.Count == 1);

        Vars["calls"].Should().MatchRawValue(1);
    }
}