using System;
using System.IO;
using System.Linq;
using CSScriptingLangGenerators.Bindings;
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
            "../../../../../CSScriptingLang/Properties",
            "../../../../../CSScriptingLang/RuntimeValues",
            // "../../../../../CSScriptingLang/RuntimeValues/Prototypes",
            "../../../../../CSScriptingLang/Lexing",
            "../../../../../CSScriptingLang/Core/Http",
            "../../../../../CSScriptingLang/Core/Async",
            "../../../../../CSScriptingLang/Interpreter/Bindings",
            "../../../../../CSScriptingLang/Interpreter/Context",
            "../../../../../CSScriptingLang/Interpreter/Modules/Libraries",
            "../../../../../CSScriptingLang/Interpreter/Execution",
            "../../../../../CSScriptingLang.Tests/InterpreterTests"
        );

        Compilation compilation = CSharpCompilation.Create(
            "CSScriptingLang",
            trees,
            new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),

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


        var generatedTrees = result.GeneratedTrees.ToList()
           .OrderBy(t => {
                if (t.FilePath.Contains("Http")) {
                    return 0;
                }
                return t.FilePath.Length;
            })
           .ToList();

        generatedTrees.ForEach(tree => {
            Console.WriteLine($" - {tree.FilePath}");
            Console.WriteLine(tree.GetText().ToString());
            Console.WriteLine("\n");
        });

        // foreach (var tree in result.GeneratedTrees) {
        //     Console.WriteLine(tree.FilePath);
        //     Console.WriteLine(tree.GetText().ToString());
        //     Console.WriteLine("\n");
        // }

    }
}