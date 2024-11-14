﻿namespace CSScriptingLang.Tests.InterpreterTests;

[TestFixture]
public class RequireTest : VirtualFS_CompilerTest
{
    [Test]
    public void RequiringScriptViaPath() {
        AddModule(
            "testModule",
            """
            print('Hello from testModule');

            function testFunction() {
                print('Hello from testFunction');
            }
            function ExportedTestFunction() {
                print('Hello from testFunction');
            }
            """
        );
        Execute(
            """
            print('Hello from main');

            var testModule = require('testModule');
            var testModule2 = require('testModule');

            testModule.ExportedTestFunction();
            // testModule.testFunction();
            """
        );

        var testModule = Vars["testModule"];

        Assert.That(testModule, Is.Not.Null);
    }
    [Test]
    public void RequiringScriptsInDirectory() {
        AddModule(
            "testModule/index.vlt",
            """
            print('Hello from testModule/index.vlt');

            function IndexFunction() {
                print('Hello from IndexFunction');
            }
            """
        );
        AddModule(
            "testModule/fileTwo.vlt",
            """
            print('Hello from testModule/fileTwo.vlt');
            
            function FileTwoFunction() {
                print('Hello from FileTwoFunction');
            }
            """
        );

        Execute(
            """
            print('Hello from main');

            var testModules = require('testModule');
            
            inspect(testModules.modules.getKeys());
            inspect(testModules.modules.getValues());
            inspect(testModules.exports);
            
            """
        );

        var testModules = Vars["testModules"];
        var exports     = testModules["exports"];
        var modules     = testModules["modules"];

        Assert.That(testModules, Is.Not.Null);
    }
    [Test]
    public void RequiringScriptsWithTopLevelAwait() {
        AddModule(
            "testModule/index.vlt",
            """
            async function IndexFunction() {
                print('Hello from IndexFunction');
            }
            
            await IndexFunction();
            """
        );
        Execute(
            """
            var testModules = require('testModule/index.vlt');
            """
        );

        var testModules = Vars["testModules"];

        Assert.That(testModules, Is.Not.Null);
    }

}