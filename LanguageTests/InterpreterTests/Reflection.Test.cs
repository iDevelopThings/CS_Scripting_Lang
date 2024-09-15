using CSScriptingLang.RuntimeValues;

namespace LanguageTests.InterpreterTests;

[TestFixture]
public class ReflectionTest : BaseCompilerTest
{
    [Test]
    public void ReflectType_Struct() {
        Execute(
            """
            type MyStructWithFields struct {
                name string
                age  int
            }

            var t = reflect<MyStructWithFields>();

            for (var (idx, field) = range t.fields) {
                print("Field; name={0} type={1}", field.name, field.type);
            }
            
            """
        );

        var t = Variables["t"];
        
        Assert.That(t, Is.Not.Null);
        
    }
    
}