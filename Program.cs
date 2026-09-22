
using web_app_scratch;
using web_app_scratch.Core;

var builder = WebApplicationFactory.CreateBuilder();
builder.Services.AddTransient<ITest, Test>();
var app = builder.Build();

app.MapGet("/test", (RequestContext context) =>
{
    var test = app.Services.GetRequiredService<ITest>();
    test.Log();
    return "Ok";
});

await app.RunAsync(5005);
// var router = new Router();
// router.MapGet("/test", (RequestContext ctx) => "Ok");
// var server = new TcpServer(5005, router);
