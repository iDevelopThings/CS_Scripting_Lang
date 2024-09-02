using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSScriptingLangGenerators.Tests;

public class AstNodeSourceGeneratorTests
{
    [Test]
    public void Generate() {
        var generator = new AstNodeSourceGenerator();
        var driver    = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            "CSScriptingLang",
            Directory.GetFiles("../../../../../CSScriptingLang/Parsing/AST/", "*.cs")
               .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f))),
            [MetadataReference.CreateFromFile("CSScriptingLang.dll")]
        );

        var runResult = driver.RunGenerators(compilation).GetRunResult();
        
        foreach (var tree in runResult.GeneratedTrees) {
            Console.WriteLine(tree.FilePath);
            Console.WriteLine(tree.GetText().ToString());
            Console.WriteLine("\n");
        }
        
        // Assert that the generated file contains the expected code.
        // Assert.Contains("public bool IsIdentifier => Type.HasAny(TokenType.Identifier);", generatedFileSyntax.GetText().ToString());
    }
}