module "main";

type MyStruct struct { }

type MyStructWithFields struct {
    name string
    age  int
    struct MyStruct
}

var inst = new<MyStructWithFields>();
        
inspect(inst);
