namespace CSScriptingLang.Tests.InterpreterTests;

[TestFixture]
public class JsonTest : VirtualFS_CompilerTest
{
    [Test]
    public void JsonSerialize() {
        Execute(
            """
            var obj = {
                int     : 1,
                float   : 1.5f,
                double  : 1.5,
                string  : 'hello',
                bool    : true,
                null    : null,
                obj     : {int : 2},
                arr     : [1, 2, 3]
            };

            var jsonString = Json.serialize(obj);

            var jsonString2 = obj.toJson();

            """
        );

        var jsonString = Vars["jsonString"];

        Assert.That(
            (string) jsonString,
            Is.EqualTo("{\"int\":1,\"float\":1.5,\"double\":1.5,\"string\":\"hello\",\"bool\":true,\"null\":null,\"obj\":{\"int\":2},\"arr\":[1,2,3]}")
        );

        var jsonString2 = Vars["jsonString2"];

        Assert.That((string) jsonString2, Is.EqualTo((string) jsonString));
    }

    [Test]
    public void JsonDeserialize() {
        Execute(
            """
            var obj = {
                int     : 1,
                float   : 1.5f,
                double  : 1.5,
                string  : 'hello',
                bool    : true,
                null    : null,
                obj     : {int : 2},
                arr     : [1, 2, 3]
            };

            var jsonString = Json.serialize(obj);
            var obj2 = Json.deserialize(jsonString);

            var obj3 = {};
            var obj4 = obj3.fromJson(jsonString);

            """
        );

        var jsonString = Vars["jsonString"];
        var jsonObj    = Vars["obj2"];

        var jsonObj3 = Vars["obj3"];
        var jsonObj4 = Vars["obj4"];

        Assert.That(
            (string) jsonString,
            Is.EqualTo("{\"int\":1,\"float\":1.5,\"double\":1.5,\"string\":\"hello\",\"bool\":true,\"null\":null,\"obj\":{\"int\":2},\"arr\":[1,2,3]}")
        );

        Assert.That((int) jsonObj["int"], Is.EqualTo(1));
        Assert.That((float) jsonObj["float"], Is.EqualTo(1.5f));
        Assert.That((float) jsonObj["double"], Is.EqualTo(1.5));
        Assert.That((string) jsonObj["string"], Is.EqualTo("hello"));
        Assert.That((bool) jsonObj["bool"], Is.True);
        Assert.That(jsonObj["null"].GetUntypedValue(), Is.Null);
        Assert.That((int) jsonObj["obj"]["int"], Is.EqualTo(2));
        Assert.That((int) jsonObj["arr"][0], Is.EqualTo(1));
        Assert.That((int) jsonObj["arr"][1], Is.EqualTo(2));
        Assert.That((int) jsonObj["arr"][2], Is.EqualTo(3));

    }


}