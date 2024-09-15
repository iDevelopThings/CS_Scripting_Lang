using System;
using System.IO;
using System.Linq;
using CSScriptingLangGenerators.Tests.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSScriptingLangGenerators.Tests;

public class AstNodeSourceGeneratorTests
{
    [Test]
    public void Generate() {
        var generator = new AstNodeSourceGenerator();
        var driver    = CSharpGeneratorDriver.Create(generator);
        var trees = CompilationUtils.GetSyntaxTrees(
            "../../../../../CSScriptingLang/Parsing/AST/",
            "../../../../../CSScriptingLang/Interpreter/Execution/"
        );

        var compilation = CSharpCompilation.Create(
            "CSScriptingLang",
            trees,
            new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile("../../../../../CSScriptingLang/bin/Debug/net8.0/CSScriptingLang.dll"),
            }
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