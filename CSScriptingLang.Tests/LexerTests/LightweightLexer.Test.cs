using System.Diagnostics;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Tests.ParserTests;

public class LexerTest
{
    public static void TimeSolutions(
        Action solution1,
        Action solution2,
        int    runs = 1000
    ) {
        var sw1 = new Stopwatch();
        var sw2 = new Stopwatch();

        var times1 = new List<TimeSpan>();
        var times2 = new List<TimeSpan>();

        for (int i = 0; i < runs; i++) {
            sw1.Start();
            solution1();
            sw1.Stop();

            sw2.Start();
            solution2();
            sw2.Stop();

            times1.Add(sw1.Elapsed);
            times2.Add(sw2.Elapsed);

            sw1.Reset();
            sw2.Reset();
        }

        string timeStr(double timeVal) {
            // if we can ouput us, do it
            if (timeVal < 1000) {
                return $"{timeVal}us";
            }

            return $"{timeVal / 1000}ms";
        }

        Console.WriteLine(
            $"Solution 1: avg = {timeStr(times1.Average(t => t.TotalMilliseconds))} min = {timeStr(times1.Min(t => t.TotalMilliseconds))} max = {timeStr(times1.Max(t => t.TotalMilliseconds))}");
        Console.WriteLine(
            $"Solution 2: avg = {timeStr(times2.Average(t => t.TotalMilliseconds))} min = {timeStr(times2.Min(t => t.TotalMilliseconds))} max = {timeStr(times2.Max(t => t.TotalMilliseconds))}");

        Console.WriteLine($"Fastest is: {(times1.Min() < times2.Min() ? "Solution 1" : "Solution 2")}");

    }

    [Test]
    public void OperatorTokens() {
        var src =
            """
            + += ++ - -= -- / * % ^ == != > < >= <= && & | || ! =
            """;

        var lexerStream = new LexerTokenStream(src);

        // "module" token should have `.KeywordType` set to `KeywordType.Module`

        // Assert `Type` & `Keyword` types in single statement
        Assert.That(lexerStream.Tokens[0].IsOp(OperatorType.Plus));
        Assert.That(lexerStream.Tokens[1].IsOp(OperatorType.PlusEquals));
        Assert.That(lexerStream.Tokens[2].IsOp(OperatorType.Increment));
        Assert.That(lexerStream.Tokens[3].IsOp(OperatorType.Minus));
        Assert.That(lexerStream.Tokens[4].IsOp(OperatorType.MinusEquals));
        Assert.That(lexerStream.Tokens[5].IsOp(OperatorType.Decrement));
        Assert.That(lexerStream.Tokens[6].IsOp(OperatorType.Divide));
        Assert.That(lexerStream.Tokens[7].IsOp(OperatorType.Multiply));
        Assert.That(lexerStream.Tokens[8].IsOp(OperatorType.Modulus));
        Assert.That(lexerStream.Tokens[9].IsOp(OperatorType.BitXor));
        Assert.That(lexerStream.Tokens[10].IsOp(OperatorType.Equals));
        Assert.That(lexerStream.Tokens[11].IsOp(OperatorType.NotEquals));
        Assert.That(lexerStream.Tokens[12].IsOp(OperatorType.GreaterThan));
        Assert.That(lexerStream.Tokens[13].IsOp(OperatorType.LessThan));
        Assert.That(lexerStream.Tokens[14].IsOp(OperatorType.GreaterThanOrEqual));
        Assert.That(lexerStream.Tokens[15].IsOp(OperatorType.LessThanOrEqual));
        Assert.That(lexerStream.Tokens[16].IsOp(OperatorType.And));
        Assert.That(lexerStream.Tokens[17].IsOp(OperatorType.BitwiseAnd));
        Assert.That(lexerStream.Tokens[18].IsOp(OperatorType.Pipe));
        Assert.That(lexerStream.Tokens[19].IsOp(OperatorType.Or));
        Assert.That(lexerStream.Tokens[20].IsOp(OperatorType.Not));
        Assert.That(lexerStream.Tokens[21].IsOp(OperatorType.Assignment));


    }


    [Test]
    public void TokenStreams() {
        var files = Directory.GetFiles(@"F:\c#\CSScriptingLang\CSScriptingLang\TestingScripts", "*.vlt", SearchOption.AllDirectories);
        var src   = string.Join("\n", files.Select(File.ReadAllText));

        // var inputStream = new InputStream(src);

        var lexerStream        = new LexerTokenStream(src);
        var tokensStreamTokens = lexerStream.Tokens;

        Assert.That(tokensStreamTokens.Count, Is.GreaterThan(0));
    }


    [Test]
    public void HandlingBlockComments() {
        var src =
            """
            function inspect(any value, string context) void; //eol?
            // def async function inspect(any value, string context) void;
            /**
            testing
            */
            var x = 1;
            """;

        var lexerStream = new LexerTokenStream(src);

        Assert.That(lexerStream.Tokens.Count, Is.EqualTo(1));

    }
    [Test]
    public void TokenPositions() {
        var src =
            """


            var testing = {};


            /**
             * Testing doc bloc
             */
            var objj = {
             a : 1,
             b : 'true',
             c : "PLS",
             d : true,
             e : { bye : { message : 'bye' }}
            };

            """;

        var lexer = new LexerTokenStream(src);
        var token = lexer.Tokens.FirstOrDefault(t => t.IsVarKeyword && t.Next.IsIdent("objj"));
        
        token.Should().NotBeNull();

    }
}