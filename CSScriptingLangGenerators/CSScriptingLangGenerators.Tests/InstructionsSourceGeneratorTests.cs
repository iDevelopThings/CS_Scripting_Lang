using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CSScriptingLangGenerators.Tests;

public class InstructionsSourceGeneratorTests
{
    [Test]
    public void GenerateInstructionPartialClass() {
        var generator = new InstructionsSourceGenerator();
        var driver    = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            "CSScriptingLang",
            [
                CSharpSyntaxTree.ParseText(File.ReadAllText("../../../../../CSScriptingLang/VM/Instructions/InstructionTypes.cs")),
                CSharpSyntaxTree.ParseText(File.ReadAllText("../../../../../CSScriptingLang/VM/Instructions/InstructionBase.cs")),
                CSharpSyntaxTree.ParseText(File.ReadAllText("../../../../../CSScriptingLang/VM/Instructions/Operand.cs")),
            ],
            []
        );

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var generatedFileSyntax = runResult.GeneratedTrees.Single(
            t => t.FilePath.EndsWith("InstructionLoadConstant.g.cs")
        );

        // Assert that the generated file contains the expected code.
        // Assert.Contains("public partial class InstructionLoadConstant\n{\n    public CSScriptingLang.VM.Instructions.OperandConstant Operand {\n        get => (CSScriptingLang.VM.Instructions.OperandConstant) _operand;\n        set => _operand = value;\n    }\n}", generatedFileSyntax.GetText().ToString());
    }
}