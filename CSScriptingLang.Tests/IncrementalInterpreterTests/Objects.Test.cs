using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class ObjectsTest : IncrementalParserTest
{
    [Test]
    public void ObjectLiteral_Basic() {
        Execute(
            """
            
                        var obj = {name: 'John', age: 20};
                    
            """
        );

        Assert.That(Variables["obj"], Is.Not.Null);
        Assert.Multiple(() => {
            Assert.That(Variables["obj"].Val.Type, Is.EqualTo(RTVT.Object));
            Assert.That(Variables["obj"].Val.GetMember("name").GetUntypedValue(), Is.EqualTo("John"));
            Assert.That(Variables["obj"].Val.GetMember("name").Type, Is.EqualTo(RTVT.String));
            Assert.That(Variables["obj"].Val.GetMember("age").GetUntypedValue(), Is.EqualTo(20));
            Assert.That(Variables["obj"].Val.GetMember("age").Type, Is.EqualTo(RTVT.Int32));

        });
    }
    [Test]
    public void ObjectLiteral_Nested() {
        Execute(
            """
            
                        var obj = {
                            name: 'John',
                            age: 20,
                            child : {
                                name: 'Jane',
                            }
                        };  
                    
            """
        );

        var obj = Variables["obj"];
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.Val.Type, Is.EqualTo(RTVT.Object));

        Assert.That(obj.Val.GetMember("name").GetUntypedValue(), Is.EqualTo("John"));
        Assert.That(obj.Val.GetMember("name").Type, Is.EqualTo(RTVT.String));

        Assert.That(obj.Val.GetMember("age").GetUntypedValue(), Is.EqualTo(20));
        Assert.That(obj.Val.GetMember("age").Type, Is.EqualTo(RTVT.Int32));

        var child = obj.Val.GetMember("child");
        Assert.That(child, Is.Not.Null);

        Assert.Multiple(() => {
            Assert.That(child.Type, Is.EqualTo(RTVT.Object));
            Assert.That(child.GetMember("name").GetUntypedValue(), Is.EqualTo("Jane"));
            Assert.That(child.GetMember("name").Type, Is.EqualTo(RTVT.String));
        });

        var pathChild = obj.Val.GetMemberByPath("child.name");
        Assert.That(pathChild, Is.Not.Null);
    }
    [Test]
    public void ObjectLiteral_WithFunction() {
        Execute(
            """
            
                        var obj = {
                            greet: function() {
                                return 'Hello ' /* + this.name*/ ;
                            }
                        };  
            
                        var callingRegular = obj.greet(); 
                        print('callingRegular', callingRegular);
            
                        var fnIndexAccess = obj['greet'];
                        var callingIndexAccess = fnIndexAccess();
                        print('callingIndexAccess', callingIndexAccess);
                        
                        var fnPropAccess = obj.greet; 
                        var callingPropAccess = fnPropAccess();
                        print('callingPropAccess', callingPropAccess);
            
                    
            """
        );

        Assert.Multiple(() => {
            Assert.That(Variables["fnPropAccess"], Is.Not.Null);
            Assert.That(Variables["fnIndexAccess"], Is.Not.Null);
            Assert.That(Variables["callingPropAccess"], Is.Not.Null);
            Assert.That(Variables["callingIndexAccess"], Is.Not.Null);
            Assert.That(Variables["callingRegular"], Is.Not.Null);
        });
    }
    [Test]
    public void ObjectLiteral_GettingProperties() {
        Execute(
            """
            
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
                    
            """
        );

        var obj = Variables["obj"];
        Assert.That(obj, Is.Not.Null);
        Assert.That(Variables["name"].RawValue, Is.EqualTo("John"));
        Assert.That(Variables["name"].RawValue, Is.EqualTo(obj.Val.GetMemberByPath("name").GetUntypedValue()));
        Assert.That(Variables["childName"].RawValue, Is.EqualTo("Jane"));
        Assert.That(Variables["childName"].RawValue, Is.EqualTo(obj.Val.GetMemberByPath("child.name").GetUntypedValue()));
        Assert.That(Variables["deeperName"].RawValue, Is.EqualTo("Jill"));
        Assert.That(Variables["deeperName"].RawValue, Is.EqualTo(obj.Val.GetMemberByPath("child.deeper.name").GetUntypedValue()));

    }
    [Test]
    public void ObjectLiteral_SettingProperties() {
        Execute(
            """
            
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
                    
            """
        );

        var obj = Variables["obj"];
        Assert.That(obj, Is.Not.Null);
        var nameField = obj.Val.GetMember("name");
        Assert.That(nameField, Is.Not.Null);


        Assert.That(obj.Val.GetMemberByPath("name").GetUntypedValue(), Is.EqualTo("Jack"));
        Assert.That(obj.Val.GetMemberByPath("child.name").GetUntypedValue(), Is.EqualTo("Janet"));
        Assert.That(obj.Val.GetMemberByPath("child.deeper.name").GetUntypedValue(), Is.EqualTo("Jillie"));
    }
    [Test]
    public void ObjectLiteral_GettingProperties_ArrayIndexer() {
        Execute(
            """
            
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
                    
            """
        );

        var obj = Variables["obj"];

        Assert.That(Variables["name"].RawValue, Is.EqualTo("John"));
        Assert.That(Variables["name"].RawValue, Is.EqualTo(obj.Val.GetMemberByPath("name").GetUntypedValue()));

        Assert.That(Variables["childName"].RawValue, Is.EqualTo("Jane"));
        Assert.That(Variables["childName"].RawValue, Is.EqualTo(obj.Val.GetMemberByPath("child.name").GetUntypedValue()));

        Assert.That(Variables["deeperName"].RawValue, Is.EqualTo("Jill"));
        Assert.That(Variables["deeperName"].RawValue, Is.EqualTo(obj.Val.GetMemberByPath("child.deeper.name").GetUntypedValue()));

    }
    [Test]
    public void ObjectLiteral_SettingProperties_ArrayIndexer() {
        Execute(
            """
            
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
                    
            """
        );

        var obj = Variables.GetValue("obj");
        Assert.That(obj, Is.Not.Null);
        var child = obj.GetMember("child");
        Assert.That(child, Is.Not.Null);
        var deeper = child.GetMember("deeper");
        Assert.That(deeper, Is.Not.Null);

        Assert.That(obj.GetMemberByPath("name").GetUntypedValue(), Is.EqualTo("Jack"));
        Assert.That(obj["name"].GetUntypedValue(), Is.EqualTo("Jack"));
        Assert.That(obj["name"], Is.EqualTo((Value) "Jack"));

        Assert.That(obj.GetMemberByPath("child.name").GetUntypedValue(), Is.EqualTo("Janet"));
        Assert.That(child["name"].GetUntypedValue(), Is.EqualTo("Janet"));

        Assert.That(obj.GetMemberByPath("child.deeper.name").GetUntypedValue(), Is.EqualTo("Jillie"));
        Assert.That(deeper["name"].GetUntypedValue(), Is.EqualTo("Jillie"));

    }

    [Test]
    public void ObjectProto_SetPrototype() {
        Execute(
            """
            var obj = {name: 'John'};
            var obj2 = {age: 20};

            obj2.setPrototype(obj);

            inspect(obj2);

            """
        );

        var v = Vars["obj2"];
        v.Should().NotBeNull();
        v.GetMember("name").Should().MatchRawValue("John");
        v.GetMember("age").Should().MatchRawValue(20);
    }

    [Test]
    public void ObjectProto_Get() {
        Execute(
            """
            
                        var obj = {name: 'John'};  
                        var v = obj.get('name');
                    
            """
        );

        var v = Vars["v"];
        v.Should().NotBeNull();
        v.Should().MatchRawValue("John");
    }
    [Test]
    public void ObjectProto_Set() {
        Execute(
            """
            
                        var obj = {name: 'John'};  
                        obj.set('name', 'Jack');
                    
            """
        );

        var obj = Vars["obj"];
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.GetMember("name"), Is.ValueType(Value.String("Jack")));
    }

    [Test]
    public void ObjectProto_Clear() {
        Execute(
            """
            
                        var obj = {name: 'John'};  
                        obj.clear();
                    
            """
        );

        var obj = Vars["obj"];
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.GetMember("name"), Is.ValueType(Value.Null()));
    }
    [Test]
    public void ObjectProto_ContainsKeyAndValue() {
        Execute(
            """
            
                        var obj = {name: 'John'};  
                        var containsKey = obj.containsKey('name');
                        var containsValue = obj.containsValue('John');
                    
            """
        );

        var obj = Vars["obj"];
        Assert.That(obj, Is.Not.Null);
        Assert.That(Vars["containsKey"], Is.ValueType(true));
        Assert.That(Vars["containsValue"], Is.ValueType(true));
    }
}