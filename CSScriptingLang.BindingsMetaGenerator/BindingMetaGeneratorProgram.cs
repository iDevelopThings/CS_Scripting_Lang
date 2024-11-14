using CSScriptingLang.BindingsMetaGenerator;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Core;
using CSScriptingLang.Interpreter;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLangGenerators.Bindings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

var path     = @"F:\c#\CSScriptingLang\CSScriptingLang";
var metaPath = Path.Join(path, "BindingsMeta.json");

var generator = new BindingsGenerator();
var driver    = CSharpGeneratorDriver.Create(generator);

var sources = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
   .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)))
   .ToList();

Compilation compilation = CSharpCompilation.Create(
    "CSScriptingLang",
    sources,
    new[] {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
    }
);

driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
var result = driver.GetRunResult();

// var result = driver.RunGenerators(compilation).GetRunResult();


if (!result.Diagnostics.IsEmpty) {
    foreach (var diagnostic in diagnostics) {
        Console.Error.WriteLine(diagnostic);
    }
} else {
    var generatedTrees = result.GeneratedTrees
        // .Where(t => t.FilePath.Contains("Module.g.cs"))
       .Where(t => t.FilePath.Contains("Class.g.cs"))
       .ToList();

    generatedTrees.ForEach(tree => {
        Console.WriteLine($" - {tree.FilePath}");
        Console.WriteLine(tree.GetText().ToString());
        Console.WriteLine("\n");
    });
}

var allMeta = TypeData.AllTypeMeta.ToList();
var allPrototypes = allMeta.SelectMany(m => m is TypeMeta_Module module ? module.Prototypes : new List<TypeMeta_ClassBased>())
   .ToList();

// Only here so we can access TypesTable prototypes
_ = new Interpreter(InterpreterConfig.FileSystem);

foreach (var proto in allPrototypes) {
    var p = TypesTable.Prototypes.FirstOrDefault(p => p.FQN == proto.Name);
    if (p == null) {
        Console.WriteLine($" - {proto.Name} not found in TypesTable.Prototypes");
        continue;
    }
    proto.Aliases = p.Aliases;
}

var processor = new TypeMetaDefinitionStringCreator(allMeta);

/*var modules = allMeta
   .Where(m => m.Kind == TypeMetaKind.Module)
   .Select(m => new {
        m,
        moduleClasses = allMeta
           .Where(c => c.Module == m.Name && c.Kind == TypeMetaKind.Class)
           .ToList(),
    })
   .ToList();*/

var workingDir = Path.GetFullPath(
    Path.Join(
        Directory.GetCurrentDirectory(),
        "..",
        "..",
        "..",
        "..",
        "CSScriptingLang",
        "bin",
#if DEBUG
        "Debug",
#else
        "Release",
#endif
        "net8.0"
    )
);

var outDir = Path.Join(workingDir, "StdLibFiles");

if (Directory.Exists(outDir)) {
    Directory.Delete(outDir, true);
}

Directory.CreateDirectory(outDir);

foreach (var m in allMeta) {
    if (m is not TypeMeta_Module module) {
        continue;
    }

    var modulePath = Path.Join(outDir, module.Name);
    Directory.CreateDirectory(modulePath);

    Console.WriteLine($" Created module dir: {modulePath}");

    foreach (var moduleClass in module.Classes.Concat(module.Prototypes)) {
        var classPath = Path.Join(modulePath, moduleClass.Name + ".vlt");
        File.WriteAllText(classPath, moduleClass.Definition);

        Console.WriteLine($"  - Created module class: {classPath}");
    }

    if (module.Methods.Count > 0 || module.Constructors.Count > 0) {
        var methodsPath = Path.Join(modulePath, "Index.vlt");
        File.WriteAllText(methodsPath, module.Definition);
        Console.WriteLine($"  - Created module index.vlt: {methodsPath}");
    }
}


/*foreach (var meta in allMeta) {
    Console.WriteLine($"\n{meta.Name}({meta.Namespace})");
    Console.WriteLine($" - Kind={meta.Kind}");
    Console.WriteLine($" - {meta.Properties.Count} properties");
    Console.WriteLine($" - {meta.Methods.Count} methods");
    Console.WriteLine("\n");
}*/

var settings = new JsonSerializerSettings() {
    NullValueHandling = NullValueHandling.Include,
};
settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

var json = JsonConvert.SerializeObject(
    allMeta,
    Formatting.Indented,
    settings
);

File.WriteAllText(metaPath, json);

Console.WriteLine("--------------------");
// Console.WriteLine(json);
Console.WriteLine("--------------------");

Console.WriteLine($" - {result.GeneratedTrees.Length} trees generated");
Console.WriteLine($" - {TypeData.Modules.Count} Modules generated");
Console.WriteLine($" - Meta written to:\n{metaPath}");
Console.WriteLine("Done!");