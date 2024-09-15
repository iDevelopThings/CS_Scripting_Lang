using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSScriptingLangGenerators.Tests.Utils;

public class CompilationUtils
{
    public static IEnumerable<SyntaxTree> GetSyntaxTrees(string path) {
        return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
           .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)));
    }
    public static IEnumerable<SyntaxTree> GetSyntaxTrees(params string[] paths) {
        return paths.SelectMany(GetSyntaxTrees);
    }
    public static IEnumerable<SyntaxTree> GetSyntaxTrees(IEnumerable<string> paths) {
        return paths.SelectMany(GetSyntaxTrees);
    }
}