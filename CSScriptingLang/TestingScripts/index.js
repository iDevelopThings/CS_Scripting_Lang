var http = require('http/test_middleware.vlt')
var server = new<HttpServer>('127.0.0.1:6969')


type TestJsonRequest struct {
    [Jsonable("message")]
    Message string
    [Jsonable("number")]
    Number int
    [Jsonable("BoolVal")]
    Bool bool
    [Jsonable("fromString")]
    FromString int
    
    Data Array<object>
}




/*
type ITest interface {
    GetName() string
    SetName(string name)
}

type MyEnum enum {
    One,
    Two,
    Three
}

type MyEnumTwo enum {
    Value int
    Obj object
    
    values {
        One(1, {message : 'hello world'}),
        Two(2, {message : 'hello'}),
        Three(3, {message : 'world'});        
    }
}
*/

server.router.GET('/param/{name}/say', (HttpRequest req, HttpResponse res, HttpRequestContext ctx) => {
    return {message : 'hello world! :D'};
});




server.router.use(http.TestMiddleware);



/*
server.router.use((HttpRequestContext ctx, HttpRequest req) => {
    print('middleware');
    inspect(req.body['message']);
    inspect(req.body['message'] == 'hello world');
    
    if(req.body['message'] == 'hello world') {
        print('hit handler: {0}', req.body['message']);
        return {message : 'hit handler'};
    }
    
    print('hit next: {0}', req.body['message']);
});
*/

server.router.POST('/test/json/request', (HttpRequest req, HttpResponse res, HttpRequestContext ctx) => {
    var body = req.getBody<TestJsonRequest>();
    
    return body;
});

// print('is listening? {0}', server.isListening);
await server.listen();
