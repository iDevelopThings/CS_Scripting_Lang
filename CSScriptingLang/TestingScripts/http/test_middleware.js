
function TestMiddleware(HttpRequestContext ctx, HttpRequest req) {
    print('middleware');
    // inspect(req.body['message']);
    // inspect(req.body['message'] == 'hello world');
    
    if(req.body['message'] == 'hello world') {
        print('hit handler: {0}', req.body['message']);
        return {message : 'hit handler'};
    }
    
    print('hit next: {0}', req.body['message']);
}
