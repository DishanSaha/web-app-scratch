namespace web_app_scratch.Middleware;

public delegate Task MiddlewareDelegate(RequestContext context, Func<RequestContext, Task> next);

public class PipelineBuilder
{
    private readonly List<MiddlewareDelegate> _middlewares = new();
    public PipelineBuilder Use(MiddlewareDelegate middlewareDelegate)
    {
        _middlewares.Add(middlewareDelegate);
        return this;
    }
    public Func<RequestContext, Task> Build()
    {
        Func<RequestContext, Task> pipeline = (context) =>
        {
            // Console.WriteLine("End of pipeline");
            return Task.CompletedTask;
        };

        for (var i = _middlewares.Count - 1; i >= 0; i--
        )
        {
            MiddlewareDelegate current = _middlewares[i];
            Func<RequestContext, Task> next = pipeline;
            pipeline = (ctx) => current(ctx, next);
        }
        return pipeline;
    }
}