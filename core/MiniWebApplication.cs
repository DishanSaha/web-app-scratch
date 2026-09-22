using System.Net;
using web_app_scratch.DI;
namespace web_app_scratch.Core;

using web_app_scratch.Middleware;

internal class MiniWebApplicationBuilder
{
    public CustomServiceCollection Services { get; } = new();
    public MiniWebApplication Build()
    {
        var provider = Services.BuildServiceProvider();
        return new MiniWebApplication(provider);
    }
}

internal class WebApplicationFactory
{
    public static MiniWebApplicationBuilder CreateBuilder()
    => new MiniWebApplicationBuilder();
}

internal class MiniWebApplication(ServiceProvider services)
{
    public readonly ServiceProvider Services = services;
    private readonly Router _router = new();
    private readonly PipelineBuilder _pipelineBuilder = new();

    public web_app_scratch.Core.Endpoint MapGet(string pattern, Delegate handler)
    {
        return _router.MapGet(pattern, handler);
    }

    // Middleware part---
    public MiniWebApplication Use(MiddlewareDelegate middleware)
    {
        _pipelineBuilder.Use(middleware);
        return this;
    }

    public async Task RunAsync(int port = 5005)
    {
        _pipelineBuilder.Use(async (ctx, next) =>
        {
            var response = _router.Resolve(ctx);
            ctx.Response = response;
            await next(ctx);
        });
        var pipeline = _pipelineBuilder.Build();
        var server = new TcpServer(port, pipeline);
        await server.StartAsync();
    }
}

// mini web framework-main architecture--------

// Browser
//    │
//    │ GET /
//    ▼
// TcpServer
//    │
//    │ HTTP request parse
//    ▼
// Router
//    │
//    │ "/"
//    ▼
// Endpoint
//    │
//    ▼
// Handler
//    │
//    ▼
// "Hello World"
//    │
//    ▼
// HTTP Response
//    │
//    ▼
// Browser

