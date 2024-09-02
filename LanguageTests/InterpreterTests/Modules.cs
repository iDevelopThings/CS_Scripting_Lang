using System.Diagnostics;

namespace LanguageTests.InterpreterTests;

[TestFixture]
public class Modules : BaseCompilerTest
{
    [Test]
    public void GlobalModule() {
        var interpreter = Execute(
            """
            import "global";
            """,
            printParseTree: true,
            moduleName: "main"
        );

        var globalMod   = interpreter.ModuleRegistry.GetModule("global");
        var didGetPrint = interpreter.ModuleRegistry.TryGetFunction("print", out var printFunc);

        Assert.Multiple(() => {
            Assert.That(globalMod.Name, Is.EqualTo("global"));
            Assert.That(globalMod.Registry, Is.EqualTo(interpreter.ModuleRegistry));
        });

    }

    [Test]
    public void LoadingModule() {
        var interpreter = Execute(
            """
            import "std.math";
            import "math";
            """,
            printParseTree: true,
            moduleName: "main"
        );

        Assert.That(interpreter.ModuleRegistry.HasModule("main"), Is.True);

    }

    [Test]
    public void ImportingModules() {
        FileSystem.CreateFile("custom_module/custom_module.js", @"
            var x = 10;
            function test(string message) {
                print('custom module test fn call');
                print(message);
            }
        ");
        Interpreter.ModuleRegistry.LoadModule("custom_module");

        FileSystem.CreateFile("custom_module_two/custom_module_two.js", @"
            var x = 10;
            function test(string message) {
                print('custom module test fn call');
                print(message);
            }
        ");
        Interpreter.ModuleRegistry.LoadModule("custom_module_two");

        var interpreter = ExecuteModules(
            """
            import "custom_module";

            custom_module.test("Hello, World!");
            // test("Hello, World!");

            """
        );

        Assert.Multiple(() => {
            Assert.That(interpreter.ModuleRegistry.HasModule("global"), Is.True);
            Assert.That(interpreter.ModuleRegistry.HasModule("custom_module"), Is.True);
            Assert.That(interpreter.ModuleRegistry.HasModule("main"), Is.True);
        });

        var customMod = interpreter.ModuleRegistry.GetModule("custom_module");

        Assert.That(customMod.HasVariable("x"), Is.True);
        Assert.That(customMod.HasFunction("test"), Is.True);

        if (interpreter.Symbols.GetFunctionDeclaration("test", out var testFn)) {
            Assert.That(testFn, Is.EqualTo(customMod.GetFunction("test")));
        }

        if (interpreter.Symbols.GetFunctionDeclaration("custom_module.test", out var testFn2)) {
            Assert.That(testFn2, Is.EqualTo(customMod.GetFunction("test")));
        }

    }
    
    [Test]
    public void ImportingDiskModules() {
        var interpreter = ExecuteFromDiskModules(
            source: """
                    import "custom_module";

                    custom_module.test("Hello, World!");
                    print('module global: {0}', custom_module.x);
                    custom_module.x = 20;
                    print('module global changed: {0}', custom_module.x);

                    """,
            diskRootPath: "F:\\c#\\CSScriptingLang\\CSScriptingLang\\TestingScripts",
            mainModuleName: "main"
        );

        Assert.Multiple(() => {
            Assert.That(interpreter.ModuleRegistry.HasModule("global"), Is.True);
            Assert.That(interpreter.ModuleRegistry.HasModule("custom_module"), Is.True);
            Assert.That(interpreter.ModuleRegistry.HasModule("main"), Is.True);
        });

        var customMod = interpreter.ModuleRegistry.GetModule("custom_module");

        Assert.That(customMod.HasVariable("x"), Is.True);
        Assert.That(customMod.HasFunction("test"), Is.True);

        if (interpreter.Symbols.GetFunctionDeclaration("test", out var testFn)) {
            Assert.That(testFn, Is.EqualTo(customMod.GetFunction("test")));
        }

        if (interpreter.Symbols.GetFunctionDeclaration("custom_module.test", out var testFn2)) {
            Assert.That(testFn2, Is.EqualTo(customMod.GetFunction("test")));
        }

    }
}