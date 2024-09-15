using System.Runtime.CompilerServices;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang;

internal class Initializer
{
    [ModuleInitializer]
    internal static void Init() {
        /*var initializers = new Dictionary<Type, Func<object, object>> {
            {typeof(Type), x => x},
            {typeof(TypeReference), x => new TypeReference(x as TypeReference)},
            {typeof(RuntimeType), x => x},
        };

        foreach (var pair in initializers) {
            CloneFactory.CustomInitializers[pair.Key] = pair.Value;
        }*/

    }
}