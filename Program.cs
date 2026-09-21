
using web_app_scratch;
using web_app_scratch.Core;

var router = new Router();
router.MapGet("/test", (RequestContext ctx) => "OK");
var server = new TcpServer(5005, router);

await server.StartAsync();