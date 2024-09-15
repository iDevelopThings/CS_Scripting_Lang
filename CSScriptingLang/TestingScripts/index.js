module "main";

var a = 0;

function myFunction() int {
    return a + 1;
}

function myFunctionTwo() int {
    return 'value: ' + a;
}

a = myFunction();

inspect(a);
inspect(myFunctionTwo());

