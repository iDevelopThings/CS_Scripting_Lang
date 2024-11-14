using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Utils;
using CSScriptingLangGenerators.Bindings;

namespace CSScriptingLang.BindingsMetaGenerator;

public class TypeMetaDefinitionStringCreator
{
    public TypeMetaDefinitionStringCreator(List<TypeMeta_ClassBased> allMeta) {
        Process(allMeta);
    }

    private void Process(List<TypeMeta_ClassBased> allMeta) {
        foreach (var type in allMeta) {
            Process(type);
        }
    }
    private void Process(TypeMeta_ClassBased type) {
        if (type.Kind == TypeMetaKind.Module) {
            type.Definition = CreateModuleDefinition(type);

            if (type is TypeMeta_Module module) {
                module.Classes.ForEach(Process);
                module.Prototypes.ForEach(Process);
            }
        }

        if (type.Kind is TypeMetaKind.Class or TypeMetaKind.Prototype) {
            var w = new ValueTextWriter();

            {
                var sw = w.Struct(type.Name);
                type.Properties.ForEach(p => {
                    if (p.Definition.IsNotNullOrEmpty()) {
                        sw.AddField(p.Definition, m => m.SetComment(p.Documentation?.ToString()));
                    } else {
                        sw.AddField(p.Name, p.Type.Name, p.Documentation?.ToString());
                    }
                });
                type.Constructors.ForEach(m => {
                    var mww = (ValueTextWriter.StructWriter.StructMember member) => {
                        var mw = new ValueTextWriter();
                        member.Write(mw);
                        m.Definition = mw.ToString();
                    };

                    if (m.Definition.IsNotNullOrEmpty()) {
                        sw.AddConstructor(m.Definition, mww);
                    } else {
                        sw.AddConstructor(m.Parameters.Select(p => p.ToTuple()), true, mww);
                    }
                });
                type.Methods.Where(m => m.IsGlobal == false).ForEach(m => {
                    var mww = (ValueTextWriter.StructWriter.StructMember member) => {
                        member.SetComment(m.Documentation?.ToString());

                        var mw = new ValueTextWriter();
                        member.Write(mw);
                        m.Definition = mw.ToString();

                        return member;
                    };

                    if (m.Definition.IsNotNullOrEmpty()) {
                        sw.AddMethod(m.Definition, mww);
                    } else {
                        sw.AddMethod(
                            m.Name,
                            m.Parameters.Select(p => p.ToTuple()),
                            m.ReturnType?.Name,
                            null,
                            true,
                            mww
                        );
                    }
                });

                w.Write(sw.Build(true));
            }

            type.Methods.Where(m => m.IsGlobal)
               .ForEach(m => {
                    w.WriteMethod(mw => {
                        mw.SetName(m.Name);
                        mw.SetReturnType(m.ReturnType.Name);
                        mw.AddParameters(m.Parameters.Select(p => new ValueTextWriter.StructMemberParameter(p)));
                        mw.SetIsDef(true);
                        mw.SetIsAsync(m.IsAsync);
                        mw.SetDefinition(m.Definition);
                        mw.SetComment(m.Documentation?.ToString());
                    }, (_, s) => m.Definition = s);
                });


            type.Definition = w.ToString();
        }
    }

    private string CreateModuleDefinition(TypeMeta_ClassBased type) {
        var vw = new ValueTextWriter();

        foreach (var method in type.Methods) {
            vw.WriteMethod(w => {

                w.SetName(method.Name);
                w.SetReturnType(method.ReturnType.Name);
                w.AddParameters(method.Parameters.Select(p => new ValueTextWriter.StructMemberParameter(p)));
                w.SetIsDef(true);
                w.SetIsAsync(method.IsAsync);
                w.SetDefinition(method.Definition);
                w.SetComment(method.Documentation?.ToString());

                method.Definition = w.ToString();
            });
        }


        return vw.ToString();
    }
}