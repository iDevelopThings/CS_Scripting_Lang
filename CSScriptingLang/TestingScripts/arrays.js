module "main";


// ----------------- 1. Arrays -----------------

var arr = [1, 2, 'str', {obj: true}, [1, 2, 3]];

print('arr = {0}', arr);

// ----------------- 2. Get array element -----------------

var el = arr[0];
var el2 = arr[1];

print('el = {0}, el2 = {0}', el, el2);

// ----------------- 3. Set array element -----------------

arr[0] = 5;
arr[1] = 6;

print('arr = {0}', arr);

arr.push(7);

// Push a range

arr.push(8, 9, 10);

// ----------------- 4. Array length -----------------

print('arr.length = {0}', arr.length);

// ----------------- 5. Other methods -----------------

arr.removeAt(0); // remove at index 0
arr.removeRange(2, 4); // remove from index 2 to 4

print('arr = {0}', arr);


// ----------------- 6. Array iteration -----------------

for (var (i, el) = range arr) {
    print('index {0} = {1}', i, el);
}



