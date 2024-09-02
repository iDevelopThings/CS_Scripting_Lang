using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace CSScriptingLangGenerators.Tests;

public class TokenExtensionSourceGeneratorTests
{
    [Test]
    public void GenerateTokenExtensions() {
        var generator = new TokenExtensionSourceGenerator();
        var driver    = CSharpGeneratorDriver.Create(generator);

        // We need to create a compilation with the required source code.
        var compilation = CSharpCompilation.Create(
            "CSScriptingLang",
            Directory.GetFiles("../../../../../CSScriptingLang/Lexing/", "*.cs")
               .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f))),
            []
        );


        // Run generators and retrieve all results.
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // All generated files can be found in 'RunResults.GeneratedTrees'.
        var generatedFileSyntax = runResult.GeneratedTrees.Single(
            t => t.FilePath.EndsWith("Token.g.cs")
        );

        // Assert that the generated file contains the expected code.
        Assert.That(generatedFileSyntax.GetText().ToString(), Contains.Substring("public bool IsIdentifier => Type.HasAny(TokenType.Identifier);"));
    }
}