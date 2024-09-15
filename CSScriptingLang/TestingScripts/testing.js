module "main";

var a = true;

// inspect(&a, 'a ref');

a = 'true';

// inspect(&a, 'a ref');
// inspect(a, 'a copy');

inspect(&a);

