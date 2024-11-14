using CSScriptingLang.Core.FileSystem;

namespace CSScriptingLang.Tests.Core;

[TestFixture]
[TestOf(typeof(CSScriptingLang.Core.FileSystem.FileSystem))]
public class FileSystemTest
{
    private FileSystem CreateVirtualFS() {
        var fs      = new FileSystem("./", false);
        var another = fs.CreateDirectory("another");
        another.CreateFile("test.txt");
        another.CreateFile("test2.txt");
        another.CreateFile("test3.txt");

        var anotherDir = fs.CreateDirectory("another_dir");
        anotherDir.CreateFile("test4.txt");
        anotherDir.CreateFile("test5.txt");
        anotherDir.CreateFile("test6.txt");

        return fs;
    }
    [Test]
    public void Virtual() {
        var fs = CreateVirtualFS();

        var files = fs.Files(true).ToList();
        var dirs  = fs.Directories(true).ToList();
        fs.PrintTree();

        Assert.Multiple(() => {
            Assert.That(files, Has.Count.EqualTo(6));
            Assert.That(dirs, Has.Count.EqualTo(2));
            Assert.That(fs.GetDirectory("another").Files().Count(), Is.EqualTo(3));
            Assert.That(fs.GetDirectory("another_dir").Files().Count(), Is.EqualTo(3));
        });

    }
    [Test]
    public void Virtual_MatchingByWildcardStr() {
        var fs = CreateVirtualFS();

        var test_x_files  = fs.List("another/*.txt", true).ToList();
        var test_x_files2 = fs.List("another_dir/*.txt", true).ToList();
        var recursiveDirs = fs.List("**/*.txt", true).ToList();

        Assert.Multiple(() => {
            Assert.That(test_x_files, Has.Count.EqualTo(3));
            // Test that `test_x_files` contains `test.txt`, `test2.txt`, and `test3.txt`
            Assert.That(test_x_files, Has.Exactly(1).Matches<IVirtualFile>(x => x.Name == "test.txt"));
            Assert.That(test_x_files, Has.Exactly(1).Matches<IVirtualFile>(x => x.Name == "test2.txt"));
            Assert.That(test_x_files, Has.Exactly(1).Matches<IVirtualFile>(x => x.Name == "test3.txt"));

            Assert.That(test_x_files2, Has.Count.EqualTo(3));
            
            Assert.That(recursiveDirs, Has.Count.EqualTo(6));
            Assert.That(recursiveDirs, Has.Exactly(6).Matches<IVirtualFile>(x => x.Name.EndsWith(".txt") && x.Name.StartsWith("test")));
        });

    }

    [Test]
    public void TestPhysicalFileSystem() {
        var fs    = new FileSystem("F:\\c#\\CSScriptingLang\\CSScriptingLang\\TestingScripts", true);
        
        fs.PrintTree();
        
        fs.Parent.PrintTree();
        
        /*// var fs = new FileSystem("F:\\c#\\CSScriptingLang\\CSScriptingLang\\TestingScripts", true);
        var fs = new FileSystem("F:\\c#\\CSScriptingLang\\CSScriptingLang\\TestingScripts", true);

        var files = fs.Files().ToList();
        var dirs  = fs.Directories().ToList();
        fs.PrintTree();

        var allWithJsExt = fs.List("*#1#*.vlt").ToList();

        Assert.That(files.Count, Is.EqualTo(1));*/

    }
}