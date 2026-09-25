using web_app_scratch.Middleware;
using web_app_scratch;
using web_app_scratch.Core;
using web_app_scratch.DI;
using web_app_scratch.Controllers;
using web_app_scratch.Models;


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


// Middleware register
app.Use(Logging);
app.Use(Timing);

app.MapGet("/test", (RequestContext context) =>
{
    var test = app.Services.GetRequiredService<ITest>();
    test.Log();
    return "Ok";
});

// Typed parameter — Model Binder দিয়ে bind হবে
app.MapGet("/users", (User user) =>
{
    return $"User: {user.Name}, Age: {user.Age}";
});


// Controller register
app.AddControllers(typeof(ProductController));

await app.RunAsync(5005);



// Summary Table
// Layer	কাজ
// Attribute	Method-এ routing তথ্য attach করে ([HttpGet("/x")])
// ControllerDiscovery	Reflection দিয়ে attributes খুঁজে Endpoint বানায়
// Endpoint	Path + Method + ActionMethod + Target ধরে রাখে
// Router	Endpoint register + match + invoke
// HandlerInvoker	Parameter bind + method call
// MiniWebApplication	সব একসাথে জোড়া লাগায়

