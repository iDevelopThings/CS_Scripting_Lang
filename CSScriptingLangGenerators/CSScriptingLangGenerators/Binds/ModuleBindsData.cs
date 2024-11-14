using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Binds;

public class ModuleBindsData : ClassBindsData
{
    public ModuleBindsData(GeneratorValuesContext ctx, Compilation compilation) : base(ctx, compilation) { }
}