using web_app_scratch.Middleware;
using web_app_scratch;
using web_app_scratch.Core;

// ---- Middleware definitions ----
async Task Logging(RequestContext ctx, Func<RequestContext, Task> next)
{
    Console.WriteLine($"[Logging] --> {ctx.method} {ctx.path}");
    await next(ctx);
    Console.WriteLine($"[Logging] <-- {ctx.path} response: {ctx.Response}");
}

async Task Timing(RequestContext ctx, Func<RequestContext, Task> next)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next(ctx);
    sw.Stop();
    Console.WriteLine($"[Timing] {ctx.path} took {sw.ElapsedMilliseconds}ms");
}


// ---- App setup ----
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

