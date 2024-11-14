object Test {
    name string
}

func main() {
    var test Test = Test{
        name: "test"
    }

    test<caret>;
}
