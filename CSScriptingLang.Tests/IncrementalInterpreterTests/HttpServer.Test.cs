using CSScriptingLang.Core.Http;
using RestSharp;

namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class HttpServerTest : IncrementalParserTest
{
    
    public record MessageResponse(string message);

    [Test]
    [CancelAfter(500)]
    public async Task UseMiddlewares() {
        HttpServer.CaptureResponse = true;

        Execute(
            """
            var server = new<HttpServer>('127.0.0.1:6969');   

            server.router.use((HttpRequestContext ctx, HttpRequest req) => {
                if(req.params['name'] == 'hello') {
                    print('hit handler: {0}', req.params);
                    return {message : 'hit handler'};
                }
                
                print('hit next: {0}', req.params);
            });

            server.router.GET('/test/{name}', (HttpRequest req, HttpResponse res, HttpRequestContext ctx) => {
                return {message : req.params['name']};
            });

            server.listen();

            """
        );


        var resultA = await HttpServer.InjectRequest<MessageResponse>(HttpVerb.Get, "/test/hello", new RestRequest() {
            RequestFormat = DataFormat.Json,
        });
        var resultB = await HttpServer.InjectRequest<MessageResponse>(HttpVerb.Get, "/test/bye", new RestRequest() {
            RequestFormat = DataFormat.Json,
        });

        var serverVar = Vars["server"];
        var server = (serverVar.DataObject as HttpServer)!;
        
        Assert.That(serverVar, Is.Not.Null);

        server.Stop();
    }

}