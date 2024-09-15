using System;
using System.IO;
using System.Linq;
using CSScriptingLangGenerators.RTObjects;
using CSScriptingLangGenerators.Tests.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSScriptingLangGenerators.Tests;

public class RTTypeSourceGeneratorTests
{
    [Test]
    public void RTTSourceGenerator() {
        var generator = new RTTypeSourceGenerator();
        var driver    = CSharpGeneratorDriver.Create(generator);
        var trees     = CompilationUtils.GetSyntaxTrees("../../../../../CSScriptingLang/RuntimeValues");

        var compilation = CSharpCompilation.Create("CSScriptingLang", trees);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        foreach (var tree in runResult.GeneratedTrees) {
            Console.WriteLine(tree.FilePath);
            Console.WriteLine(tree.GetText().ToString());
            Console.WriteLine("\n");
        }

        // Assert that the generated file contains the expected code.
        // Assert.Contains("public bool IsIdentifier => Type.HasAny(TokenType.Identifier);", generatedFileSyntax.GetText().ToString());
    }

    [Test]
    public void ValueFactorySourceGenerator() {
        var generator = new ValueFactorySourceGenerator();
        var driver    = CSharpGeneratorDriver.Create(generator);
        var trees     = CompilationUtils.GetSyntaxTrees("../../../../../CSScriptingLang/RuntimeValues/Values");

        var compilation = CSharpCompilation.Create("CSScriptingLang", trees);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        foreach (var tree in runResult.GeneratedTrees) {
            // if(!tree.FilePath.Contains("CastsAndOperators.g.cs")) continue;
            // if(!tree.FilePath.Contains("Extensions.g.cs")) continue;
            if (!tree.FilePath.Contains("Factory.g.cs")) continue;
            Console.WriteLine(tree.FilePath);
            Console.WriteLine(tree.GetText().ToString());
            Console.WriteLine("\n");
        }

        // Assert that the generated file contains the expected code.
        // Assert.Contains("public bool IsIdentifier => Type.HasAny(TokenType.Identifier);", generatedFileSyntax.GetText().ToString());
    }
}