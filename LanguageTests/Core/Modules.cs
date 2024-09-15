using System.Diagnostics;

namespace LanguageTests.Core;

[TestFixture]
public class Modules : BaseCompilerTest
{
    [Test]
    public void ResolvingModules() {
        FileSystem.AddFile(
            "custom_module/custom_module.js",
            """
            module "custom_module";

            var x = 10;
            function test(string message) {
                print('custom module test fn call');
                print(message);
            }
                    
            """
        );
        FileSystem.AddFile(
            "custom_module/nested/custom_module.js",
            """
            module "custom_module";

            var x2 = 10;
            function test2(string message) {
                print('custom module test fn call');
                print(message);
            }
                    
            """
        );
        Execute(
            """
            module "main";

            import "custom_module";

            function main() {
                print('main module');
            }

            custom_module.test("Hello, World!");
            test("Hello, World!");

            """
        );

        Assert.Multiple(() => {
            Assert.That(ModuleResolver.Has("custom_module"), Is.True);
            Assert.That(ModuleResolver.Has("main"), Is.True);
        });

        var customMod = ModuleResolver.Get("custom_module");

        /*
        Assert.Multiple(() => {
            Assert.That(customMod.HasVariable("x"), Is.True);
            Assert.That(customMod.HasFunction("test"), Is.True);
        });

        if (Functions.Get("test", out var testFn)) {
            Assert.That(testFn, Is.EqualTo(customMod.GetFunction("test")));
        }

        if (Functions.Get("custom_module.test", out var testFn2)) {
            Assert.That(testFn2, Is.EqualTo(customMod.GetFunction("test")));
        }
        */


    }

    [Test]
    public void ImportingDiskModules() {
        ExecuteFromDiskModules(
            source: """
                    module "main";
                    
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
            Assert.That(ModuleResolver.Has("custom_module"), Is.True);
            Assert.That(ModuleResolver.Has("main"), Is.True);
        });

        var customMod = ModuleResolver.Get("custom_module");

        /*
        Assert.That(customMod.HasVariable("x"), Is.True);
        Assert.That(customMod.HasFunction("test"), Is.True);

        if (Functions.Get("test", out var testFn)) {
            Assert.That(testFn, Is.EqualTo(customMod.GetFunction("test")));
        }

        if (Functions.Get("custom_module.test", out var testFn2)) {
            Assert.That(testFn2, Is.EqualTo(customMod.GetFunction("test")));
        }
        */

    }
    
    [Test]
    public void NativeModulePolyfills() {
        FileSystem.AddFile(
            "custom_module/nested/custom_module.js",
            """
            module "custom_module";

            var x = 10;
            function test(string message) {
                print('custom module test fn call');
                print(message);
            }
                    
            """
        );
        Execute(
            """
            module "main";

            import "custom_module";

            function main() {
                print('main module');
            }
            
            @def function push(this Array, BaseValue value);
            @def function print(string message, ...args);

            custom_module.test("Hello, World!");
            test("Hello, World!");

            """
        );

        Assert.Multiple(() => {
            Assert.That(ModuleResolver.Has("custom_module"), Is.True);
            Assert.That(ModuleResolver.Has("main"), Is.True);
        });

        var customMod = ModuleResolver.Get("custom_module");

        /*
        Assert.Multiple(() => {
            Assert.That(customMod.HasVariable("x"), Is.True);
            Assert.That(customMod.HasFunction("test"), Is.True);
        });

        if (Functions.Get("test", out var testFn)) {
            Assert.That(testFn, Is.EqualTo(customMod.GetFunction("test")));
        }

        if (Functions.Get("custom_module.test", out var testFn2)) {
            Assert.That(testFn2, Is.EqualTo(customMod.GetFunction("test")));
        }
        */


    }

}