using CSScriptingLang.RuntimeValues;

namespace CSScriptingLang.Tests.InterpreterTests;

[TestFixture]
public class TypeDeclaration_Struct_Test : VirtualFS_CompilerTest
{

    [Test]
    public void Construction() {
        Execute(
            """
            type Struct struct {
                name string
                age  int
            }

            var inst = new<Struct>();
                    
            inspect(inst);

            """
        );

        var structInst = Vars["inst"];
        Assert.That(structInst, Is.Not.Null);
    }

    [Test]
    public void ZeroValOnInit() {
        Execute(
            """
            type Struct struct {
                name string
                age  int
            }

            var inst = new<Struct>();
                    
            inspect(inst);

            """
        );

        var structInst = Vars["inst"];
        Assert.That(structInst, Is.Not.Null);

        Assert.That(structInst?.GetMember("name").As.String(), Is.EqualTo(string.Empty));
        Assert.That(structInst?.GetMember("age").As.Int(), Is.EqualTo(0));
    }
    [Test]
    public void Constructors() {
        Execute(
            """
            type Struct struct {
                name string
                age  int
                
                Struct() {
                    print('constructor(Struct()) called');
                }
                
                Struct(int age) {
                    print('constructor(Struct(int)) called');
                    this.age = age;
                }
                
                Struct(int age, string name) {
                    print('constructor(Struct(int, string)) called');
                    this.age = age;
                    this.name = name;
                }
            }

            var instA = new<Struct>(2);
            inspect(instA);

            var instB = new<Struct>(3, 'John');
            inspect(instB);
            
            var instC = new<Struct>();
            inspect(instC);

            """
        );

        var structInstA = Vars["instA"];
        Assert.Multiple(() =>
        {
            Assert.That(structInstA, Is.Not.Null);
            Assert.That(structInstA?.GetMember("name").As.String(), Is.EqualTo(string.Empty));
            Assert.That(structInstA?.GetMember("age").As.Int(), Is.EqualTo(2));
        });
        
        var structInstB = Vars["instB"];
        Assert.Multiple(() =>
        {
            Assert.That(structInstB, Is.Not.Null);
            Assert.That(structInstB?.GetMember("name").As.String(), Is.EqualTo("John"));
            Assert.That(structInstB?.GetMember("age").As.Int(), Is.EqualTo(3));
        });
        
        var structInstC = Vars["instC"];
        Assert.Multiple(() =>
        {
            Assert.That(structInstC, Is.Not.Null);
            Assert.That(structInstC?.GetMember("name").As.String(), Is.EqualTo(string.Empty));
            Assert.That(structInstC?.GetMember("age").As.Int(), Is.EqualTo(0));
        });
    }

    [Test]
    public void SettingData() {
        Execute(
            """

            type Struct struct {
                name string
                age  int
            }

            var inst = new<Struct>();
                    
            inspect(inst, 'var inst = new<Struct>()');

            inst.name = "John";
            inst.age = 25;
            inst.someFunc = () => {
                print("Hello from someFunc, {0} {1}", inst.name, inst.age);
                print("Hello from someFunc, {0} {1}", this.name, this.age);
                inspect(this, 'inside someFunc closure');
            };

            inspect(inst, 'pre inst.someFunc()');

            inst.someFunc();

            """
        );

        var structInst = Vars["inst"];
        Assert.That(structInst.GetMember("name").As.String(), Is.EqualTo("John"));
        Assert.That(structInst.GetMember("age").As.Int(), Is.EqualTo(25));
    }

    [Test]
    public void Methods() {
        Execute(
            """

            type Struct struct {
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

            var inst = new<Struct>();

            inst.GetName();
                    
            inspect(inst);

            inst.SetName("John");
            var n = inst.GetName();

            """
        );

        var structInst = Vars["inst"];
        Assert.That(structInst.GetMember("name").As.String(), Is.EqualTo("John"));
        Assert.That(Vars["n"].As.String(), Is.EqualTo("John"));

    }
    
    [Test]
    public void InterfaceDeclaration() {
        Execute(
            """

            type ITest interface {
                GetName() string
                SetName(string name)
            }
            type Struct struct {
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

            var inst = new<Struct>();

            inst.GetName();
                    
            inspect(inst);

            inst.SetName("John");
            var n = inst.GetName();

            """
        );

        var structInst = Vars["inst"];
        Assert.That(structInst.GetMember("name").As.String(), Is.EqualTo("John"));
        Assert.That(Vars["n"].As.String(), Is.EqualTo("John"));

    }
    [Test]
    public void StructWithDefs() {
        Execute(
            """
            
            type Array struct
            {
              def push(int pls, int plspsl) int32;
              def removeAt(object index) int32;
              def removeRange(object start, object end) int32;
              def getEnumerator() object;
            }
            

            """
        );

    }
}