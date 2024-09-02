









var obj = {
    name: 'John',
    greet: function() {
        print('Hello, {0}!', this.name);
    },
    arr: [1, 2, 'surprise numbah 3', 4, 5],
    child: {
        name: 'Billy',
        children: [{something:true}]
    }
}

obj.greet();
obj.name = 'Jane';
obj.greet();

print('arr = {0}', obj.arr);
print(obj);
/*

//var arr = [1, 2, '<bold.blue>surprise</> <yellow>numbah 3</>', 4, 5];
var arr = [1, 2, 'surprise >numbah 3', 4, 5];
for (var (i, el) = range arr) {
     print('index {0} = {1}', i, el);
}
*/
