using CSScriptingLang.RuntimeValues;
using CSScriptingLang.VM;

namespace LanguageTests.InterpreterTests;

public class ObjectsTest : BaseCompilerTest
{
    [Test]
    public void ObjectLiteral_Basic() {
        Execute(@"
            var obj = {name: 'John', age: 20};
        ");

        Assert.That(Symbols["obj"], Is.Not.Null);
        Assert.Multiple(() => {
            Assert.That(Symbols["obj"].Type.Type, Is.EqualTo(RTVT.Object));
            Assert.That(Symbols["obj"].Value.GetField("name").Value, Is.EqualTo("John"));
            Assert.That(Symbols["obj"].Value.GetField("name").Type, Is.EqualTo(RTVT.String));
            Assert.That(Symbols["obj"].Value.GetField("age").Value, Is.EqualTo((double) 20));
            Assert.That(Symbols["obj"].Value.GetField("age").Type, Is.EqualTo(RTVT.Number));
        });
    }
    [Test]
    public void ObjectLiteral_Nested() {
        Execute(@"
            var obj = {
                name: 'John',
                age: 20,
                child : {
                    name: 'Jane',
                }
            };  
        ");

        var obj = Symbols["obj"];
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.Type.Type, Is.EqualTo(RTVT.Object));

        Assert.That(obj.Value.GetField("name"), LangIs.RuntimeValue("John"));
        Assert.That(obj.Value.GetField("age"), LangIs.RuntimeValue((double) 20));

        var child = obj.Value.GetField("child");
        Assert.That(child, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(child.Type, Is.EqualTo(RTVT.Object));
            Assert.That(child.GetField("name"), LangIs.RuntimeValue("Jane"));
        });

        var pathChild = obj.Value.GetFieldByPath("child.name");
        Assert.That(pathChild, Is.Not.Null);
    }
    [Test]
    public void ObjectLiteral_WithFunction() {
        Execute(@"
            var obj = {
                greet: function() {
                    return 'Hello ' /* + this.name*/ ;
                }
            };  

            var fnIndexAccess = obj['greet'];
            var callingIndexAccess = fnIndexAccess();
            
            var fnPropAccess = obj.greet; 
            var callingPropAccess = fnPropAccess();

            var callingRegular = obj.greet(); 
        ");

        Assert.Multiple(() =>
        {
            Assert.That(Symbols["fnPropAccess"], Is.Not.Null);
            Assert.That(Symbols["fnIndexAccess"], Is.Not.Null);
            Assert.That(Symbols["callingPropAccess"], Is.Not.Null);
            Assert.That(Symbols["callingIndexAccess"], Is.Not.Null);
            Assert.That(Symbols["callingRegular"], Is.Not.Null);
        });
    }
    [Test]
    public void ObjectLiteral_GettingProperties() {
        Execute(@"
            var obj = {
                name: 'John',
                age: 20,
                child : {
                    name: 'Jane',
                    deeper: {
                        name: 'Jill'
                    }
                }
            };  
            
            var name = obj.name;
            var childName = obj.child.name;
            var deeperName = obj.child.deeper.name;
        ");

        var obj = Symbols["obj"];
        Assert.That(obj, Is.Not.Null);
        Assert.That(Symbols["name"].RawValue, Is.EqualTo("John"));
        Assert.That(Symbols["name"].RawValue, Is.EqualTo(obj.Value.GetFieldByPath("name").Value));
        Assert.That(Symbols["childName"].RawValue, Is.EqualTo("Jane"));
        Assert.That(Symbols["childName"].RawValue, Is.EqualTo(obj.Value.GetFieldByPath("child.name").Value));
        Assert.That(Symbols["deeperName"].RawValue, Is.EqualTo("Jill"));
        Assert.That(Symbols["deeperName"].RawValue, Is.EqualTo(obj.Value.GetFieldByPath("child.deeper.name").Value));

    }
    [Test]
    public void ObjectLiteral_SettingProperties() {
        Execute(@"
            var obj = {
                name: 'John',
                age: 20,
                child : {
                    name: 'Jane',
                    deeper: {
                        name: 'Jill'
                    }
                }
            };  
            
            obj.name = 'Jack';
            obj.child.name = 'Janet';
            obj.child.deeper.name = 'Jillie';
        ");

        var obj = Symbols["obj"];
        Assert.That(obj, Is.Not.Null);
        var nameField = obj.Value.GetField("name");
        Assert.That(nameField, Is.Not.Null);


        Assert.That(obj.Value.GetFieldByPath("name").Value, Is.EqualTo("Jack"));
        Assert.That(obj.Value.GetFieldByPath("child.name").Value, Is.EqualTo("Janet"));
        Assert.That(obj.Value.GetFieldByPath("child.deeper.name").Value, Is.EqualTo("Jillie"));
    }
    [Test]
    public void ObjectLiteral_GettingProperties_ArrayIndexer() {
        Execute(@"
            var obj = {
                name: 'John',
                age: 20,
                child : {
                    name: 'Jane',
                    deeper: {
                        name: 'Jill'
                    }
                }
            };  
            
            var name = obj['name'];
            var childName = obj['child']['name'];
            var deeperName = obj['child']['deeper']['name'];
        ");

        var obj = Symbols["obj"];
        Assert.NotNull(obj);

        Assert.That(Symbols["name"].RawValue, Is.EqualTo("John"));
        Assert.That(Symbols["name"].RawValue, Is.EqualTo(obj.Value.GetFieldByPath("name").Value));

        Assert.That(Symbols["childName"].RawValue, Is.EqualTo("Jane"));
        Assert.That(Symbols["childName"].RawValue, Is.EqualTo(obj.Value.GetFieldByPath("child.name").Value));

        Assert.That(Symbols["deeperName"].RawValue, Is.EqualTo("Jill"));
        Assert.That(Symbols["deeperName"].RawValue, Is.EqualTo(obj.Value.GetFieldByPath("child.deeper.name").Value));

    }
    [Test]
    public void ObjectLiteral_SettingProperties_ArrayIndexer() {
        Execute(@"
            var obj = {
                name: 'John',
                age: 20,
                child : {
                    name: 'Jane',
                    deeper: {
                        name: 'Jill'
                    }
                }
            };  
            
            obj['name'] = 'Jack';
            obj['child']['name'] = 'Janet';
            obj['child']['deeper']['name'] = 'Jillie';
        ");

        var obj = Symbols["obj"];
        Assert.That(obj, Is.Not.Null);

        Assert.That(obj.Value.GetFieldByPath("name").Value, Is.EqualTo("Jack"));
        Assert.That(obj.Value.GetFieldByPath("child.name").Value, Is.EqualTo("Janet"));
        Assert.That(obj.Value.GetFieldByPath("child.deeper.name").Value, Is.EqualTo("Jillie"));
    }
}