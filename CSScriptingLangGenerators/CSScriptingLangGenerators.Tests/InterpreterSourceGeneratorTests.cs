using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace CSScriptingLangGenerators.Tests;

public class InterpreterSourceGeneratorTests
{

    [Test]
    public void Generate() {
        var generator = new InterpreterSourceGenerator();
        var driver    = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            "CSScriptingLang",
            Directory.GetFiles("../../../../../CSScriptingLang/Parsing/AST/", "*.cs")
               .Concat(Directory.GetFiles("../../../../../CSScriptingLang/Interpreter/", "*.cs"))
               .Concat(Directory.GetFiles("../../../../../CSScriptingLang/VM/", "*.cs"))
               .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f))),
            []
        );

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var generatedFileSyntax = runResult.GeneratedTrees.Single(
            t => t.FilePath.EndsWith("Interpreter.g.cs")
        );

        Console.WriteLine(generatedFileSyntax.GetText().ToString());

        // Assert that the generated file contains the expected code.
        // Assert.Contains("public bool IsIdentifier => Type.HasAny(TokenType.Identifier);", generatedFileSyntax.GetText().ToString());
    }
}