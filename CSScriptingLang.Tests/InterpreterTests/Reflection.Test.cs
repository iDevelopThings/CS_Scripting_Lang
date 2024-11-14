using CSScriptingLang.RuntimeValues;

namespace CSScriptingLang.Tests.InterpreterTests;

[TestFixture]
public class ReflectionTest : VirtualFS_CompilerTest
{
    [Test]
    public void ReflectValue_Obj() {
        Execute(
            """
            var obj = {name: "John", age: 30};
            type MyStructWithFields struct {
                name string
                age  int
            }
            var struct = new<MyStructWithFields>();

            var structType = getType(struct);
            var type = getType(obj);
            """
        );

        var structType = Vars["structType"];
        var type       = Vars["type"];

        Assert.That(type, Is.Not.Null);

    }
    [Test]
    public void ReflectType_Struct() {
        Execute(
            """
            type MyStructWithFields struct {
                name string
                age  int
            }

            var t = getType<MyStructWithFields>();

            for (var fieldName = range t.fields) {
                print("Field; name={0}", fieldName);
            }
            for (var (idx, field) = range t.fieldValues) {
                print("Field; name={0} type={1}", field.name, field.type);
            }
            

            """
        );

        var t = Vars["t"];

        Assert.That(t, Is.Not.Null);

    }

}