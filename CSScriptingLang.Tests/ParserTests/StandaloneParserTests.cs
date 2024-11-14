using CSScriptingLang.IncrementalParsing;
using CSScriptingLang.Parsing;

namespace CSScriptingLang.Tests.ParserTests;

[LSPTest]
public class StandaloneParserTests : IncrementalParserTest
{
    [Test]
    [CancelAfter(2000)]
    public void Incremental() {
        var files = Directory.GetFiles(@"F:\c#\CSScriptingLang\CSScriptingLang\TestingScripts", "*.vlt", SearchOption.AllDirectories);

        var sources = files.Select(
            f => AddScript(f, File.ReadAllText(f))
        ).ToDictionary(s => s.file.Rel, s => s);

        Console.WriteLine("Added scripts");
    }
    [Test]
    [CancelAfter(2000)]
    public void IncrementalSingle() {
        // var script = AddScriptFromOS(@"F:\c#\CSScriptingLang\CSScriptingLang\TestingScripts\another.vlt");
        var script = AddScriptFromOS(@"F:\c#\CSScriptingLang\CSScriptingLang\TestingScripts\another_simple.vlt");
        var tree   = script.SyntaxTree;
        
        Console.WriteLine("Added script");
    }
    [Test]
    public void StandaloneParser() {
        var files = Directory.GetFiles(@"F:\c#\CSScriptingLang\CSScriptingLang\TestingScripts", "*.vlt", SearchOption.AllDirectories);
        var src   = string.Join("\n", files.Select(File.ReadAllText));

        var standaloneParser  = new Parser(src);
        var standaloneProgram = standaloneParser.Parse(false);

        var incrementalParser = new IncrementalParser(src);
        incrementalParser.Parse();

        // var lexer   = new Lexer(src);
        // var parser  = new Parser(lexer, false);
        // var program = parser.Parse(false);

        LexerTest.TimeSolutions(
            () => {
                standaloneParser  = new Parser(src);
                standaloneProgram = standaloneParser.Parse(false);
            },
            () => {
                incrementalParser = new IncrementalParser(src);
                incrementalParser.Parse();
            },
            1000
        );

        // Assert.That(standaloneProgram.Nodes.Count, Is.EqualTo(program.Nodes.Count));
        // Assert.That(standaloneProgram.Nodes, Is.EqualTo(program.Nodes));

        // var settings = new JsonSerializerSettings {
        //     TypeNameHandling = TypeNameHandling.All,
        //     // Disable reference loop handling
        //     ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        // };
        // var standaloneJson = JsonConvert.SerializeObject(standaloneProgram.Nodes, settings);
        // var json           = JsonConvert.SerializeObject(program.Nodes, settings);
        //
        // var jsonDiff = new JsonDiffPatch().Diff(standaloneJson, json);
        //
        // Assert.That(jsonDiff, Is.Null);

    }
}