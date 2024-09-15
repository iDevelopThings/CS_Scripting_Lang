using CSScriptingLang.RuntimeValues;

namespace LanguageTests.InterpreterTests;

[TestFixture]
public class TypeDeclarationsTest : BaseCompilerTest
{
    [Test]
    public void StructDeclaration_ZeroVal() {
        Execute(
            """

            type MyStruct struct { }

            type MyStructWithFields struct {
                name string
                age  int
                struct MyStruct
            }

            var inst = new<MyStructWithFields>();
                    
            inspect(inst);

            """
        );

        var structInst = Variables["inst"];
        Assert.That(structInst?.Val.GetMember("name").As.String(), Is.EqualTo(string.Empty));
        Assert.That(structInst?.Val.GetMember("age").As.Int(), Is.EqualTo(0));

    }
    [Test]
    public void StructDeclaration_SettingData() {
        Execute(
            """

            type MyStruct struct { }

            type MyStructWithFields struct {
                name string
                age  int
                struct MyStruct
                
            }

            // fn (this MyStructWithFields) someFunc () {
            //     print("Hello from someFunc, {0} {1}", this.name, this.age);
            //     this.someMethod();
            // }

            var inst = new<MyStructWithFields>();
                    
            inspect(inst);

            inst.name = "John";
            inst.age = 25;
            inst.MyStruct = new<MyStruct>();
            inst.someFunc = () => {
                print("Hello from someFunc, {0} {1}", inst.name, inst.age);
                print("Hello from someFunc, {0} {1}", this.name, this.age);
                inspect(this, 'this');
            };

            inspect(inst);

            inst.someFunc();

            """
        );

        var structInst = Variables["inst"];
        Assert.That(structInst.Val.GetMember("name").As.String(), Is.EqualTo("John"));
        Assert.That(structInst.Val.GetMember("age").As.Int(), Is.EqualTo(25));

    }
    [Test]
    public void StructDeclaration_Methods() {
        Execute(
            """

            type MyStructWithFields struct {
                name string
                
                GetName() string {
                    print('Get Name is: {0}', this.name);
                    return this.name;
                }
                
                SetName(string name) {
                    this.name = name;
                    print('Name set to: {0}', this.name);
                }
            }

            var inst = new<MyStructWithFields>();
            
            inst.GetName();
                    
            inspect(inst);

            inst.SetName("John");
            var n = inst.GetName();

            """
        );

        var structInst = Variables["inst"];

    }
}