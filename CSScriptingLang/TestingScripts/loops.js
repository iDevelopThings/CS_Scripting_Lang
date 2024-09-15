module "main";

// ----------------- for i loop -----------------

for(var i = 0; i < 4; i++) {
    print('i = {0}', i);
}

var j = 0;
for(; j < 4; j++) {
    print('j = {0}', j);
}

// ----------------- for i loop with break -----------------

for(var i = 0; i < 4; i++) {
    print('i = {0}', i);
    if(i == 2) {
        break;
    }
}

// ----------------- for range to i -----------------

for(var i = range 4) {
    print('i = {0}', i);
}

// ----------------- for range over array -----------------

var arr = [1, 2, '<bold.blue>surprise</> <yellow>numbah 3</>', 4, 5];
for (var (i, el) = range arr) {
     print('index {0} = {1}', i, el);
}