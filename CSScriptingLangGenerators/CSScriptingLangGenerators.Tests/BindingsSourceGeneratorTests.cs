using System;
using CSScriptingLangGenerators.Bindings;
using CSScriptingLangGenerators.RTObjects;
using CSScriptingLangGenerators.Tests.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSScriptingLangGenerators.Tests;

public class BindingsSourceGeneratorTests
{
    [Test]
    public void BindingsGenerator() {
        var generator = new BindingsGenerator();
        var driver    = CSharpGeneratorDriver.Create(generator);

        var trees = CompilationUtils.GetSyntaxTrees(
            "../../../../../CSScriptingLang/RuntimeValues",
            // "../../../../../CSScriptingLang/RuntimeValues/Prototypes",
            "../../../../../CSScriptingLang/Lexing",
            "../../../../../CSScriptingLang/Interpreter/Bindings",
            "../../../../../CSScriptingLang/Interpreter/Context",
            "../../../../../CSScriptingLang/Interpreter/Modules/Libraries"
        );

        Compilation compilation = CSharpCompilation.Create(
            "CSScriptingLang",
            trees,
            new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),

                // MetadataReference.CreateFromFile("../../../../../CSScriptingLang/bin/Debug/net8.0/CSScriptingLang.dll"),
            }
        );

        driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);

        var result = driver.RunGenerators(compilation).GetRunResult();

        if (!diagnostics.IsEmpty) {
            foreach (var diagnostic in diagnostics) {
                Console.Error.WriteLine(diagnostic);
            }
        }


        foreach (var tree in result.GeneratedTrees) {
            Console.WriteLine(tree.FilePath);
            Console.WriteLine(tree.GetText().ToString());
            Console.WriteLine("\n");
        }

        // Assert that the generated file contains the expected code.
        // Assert.Contains("public bool IsIdentifier => Type.HasAny(TokenType.Identifier);", generatedFileSyntax.GetText().ToString());
    }
}