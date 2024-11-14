using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class FunctionsTest : IncrementalParserTest
{
    [Test]
    public void FunctionDeclaration() {
        Execute(
            @"
            function add(int a, int b) {
                return a + b;
            }
        "
        );

        var doesExist = Variables.Get("add", out var add);
        doesExist.Should().BeTrue();
        add.Val.Should().NotBeNull();
        add.Val.As.Fn().Should()
           .NotBeNull()
           .And
           .BeOfType<FnClosure>();
    }

    [Test]
    public void WriteToParameter() {
        Execute(
            """
            var val = 1;
            function add(int v) {
                v += v;
                return v;
            }

            var result = add(val);
            """
        );

        Vars["val"].Should().MatchRawValue(1);
        Vars["result"].Should().MatchRawValue(2);
    }

    [Test]
    public void Closures() {
        Execute(
            """
            var val = 1;
            function do(int x) {
                return (int y) => x += y;
            }

            var c = do(val);
            var cResult = c(10);
            var result = c(2);
            """
        );

        var val = Vars["val"];
        val.Should().MatchRawValue(1);

        var cResult = Vars["cResult"];
        cResult.Should().MatchRawValue(11);

        var result = Vars["result"];
        result.Should().MatchRawValue(13);
    }

    [Test]
    public void TailCall() {
        Execute(
            """
            function loop(int i) {
                if (i == 0) {
                    return 'done';
                }
            
                return loop(i - 1);
            }

            loop(10);

            """
        );

    }

    [Test]
    public void FunctionExecution_ReturnValueVarStorage() {
        Execute(
            @"
            function add(int a, int b) {
                return a + b;
            }
            var result = add(10, 20);
        "
        );

        Vars["result"].Should().MatchRawValue(30);
    }

    [Test]
    public void FunctionExecution_Void() {
        Execute(
            @"
            function add(int a, int b) {
                print(a + b);
            }
            add(10, 20);
            var result = add(1, 2);
        "
        );

        var result = Variables["result"];
        var value  = result.RawValue;

        result.Val.Is.Null.Should().BeTrue();
    }

    [Test]
    public void FunctionExecution_Nested() {
        Execute(
            @"
            function add(int a, int b) {
                function sub(int c, int d) {
                    return c - d;
                }
                return a + b + sub(a, b);
            }
            var result = add(10, 20);
        "
        );

        Vars["result"].Should().MatchRawValue(20);
    }

    [Test]
    public void FunctionExecution_Nested_InheritingParentScope() {
        // TrackFunctionFrames();

        Execute(
            """
            function add(int a, int b) {
                function sub(int c) {
                    return a - c;
                }
                return a + b + sub(b);
            }
            var result = add(10, 20);
            """
        );

        Vars["a"].Should().NotBeNull();
        Vars["b"].Should().NotBeNull();
        Vars["c"].Should().NotBeNull();
        Vars["result"].Should().BeNull();
        Vars["result"].Should().MatchRawValue(20);
    }

    [Test]
    public void FunctionTypes_Equality() {

        Execute(
            @"
            var a = function(int a, int b) { print('hi'); }
            var b = function(int a, int b) { print('bye'); }

            var eq = a == b; // false?
            var aEq = a == a; // true?
            var bEq = b == b; // true?
            
            function add(int a, int b) { print('hi'); }
            function sub(int a, int b) { print('bye'); }

            var addEq = add == sub; // false?
            var addEq2 = add == add; // true?

            var aCopy = a;
            var aEqCopy = a == aCopy; // true?
            
        "
        );

        var a = Vars["a"];
        var b = Vars["b"];
        a.Should().NotBeSameAs(b);

        var eq = Vars["eq"];
        eq.Should().MatchRawValue(false);

        var aEq = Vars["aEq"];
        aEq.Should().MatchRawValue(true);
        var bEq = Vars["bEq"];
        bEq.Should().MatchRawValue(true);
        var addEq = Vars["addEq"];
        addEq.Should().MatchRawValue(false);
        var addEq2 = Vars["addEq2"];
        addEq2.Should().MatchRawValue(true);
        var aCopy = Vars["aCopy"];
        aCopy.Should().MatchValue(a);

    }

    [Test]
    public void LambdaFunction() {
        Execute(
            """
            var a = (int a, int b) => { print('hi'); };
            inspect(a);
            a(0,0);
                              
            var (x, y) = (10, 20);
            inspect(x);
            inspect(y);
             
            var z = (1 + 2) * 3;
            inspect(z);

            """
        );

        var a = Vars["a"];
        a.Should().NotBeNull().And.BeOfType<Value>();
    }

    [Test]
    [TestCase(1, 1, 2)]
    [TestCase(2, 2, 0)]
    public void ReturnStatements_InIfStatement(int a, int b, int expected) {
        Execute(
            $$"""
              function add(int a, int b) {
                  if(a + b == 2) {
                      print('(if) a={0}, b={1}', a, b);
                      return a + b;
                  }
                  print('(else) a={0}, b={1}', a, b);
                  return 0;
              }
              var result = add({{a}}, {{b}});
              """
        );
        Vars["result"].Should().MatchRawValue(expected);
    }

    [Test]
    public void SequenceFunction() {
        Execute(
            $$"""
              seq function fn() {
                yield 1;
                yield 2;
                yield 3;
              }
              var values = fn();

              for(var v = range values) {
                print(v);
              }

              values = fn();

              """
        );

        var values = Vars["values"];
        values.IsEnumerable.Should().BeTrue();

        var expected = new[] {Value.Int32(1), Value.Int32(2), Value.Int32(3)};
        values.Enumerate(Ctx).Should().Equal(expected);
    }
    [Test]
    public void AsyncFunction() {
        Execute(
            $$"""
              async function fn() {
                return 0;
              }
              var result = await fn();
              """
        );

        var values = Vars["result"];
    }
}