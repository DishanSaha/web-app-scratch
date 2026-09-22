namespace web_app_scratch.Core;

public class Endpoint
{
    private readonly string Path;
    private readonly string Method;
    public readonly Delegate Handler;

    public Endpoint(string path, string method, Delegate handler)
    {
        Path = path;
        Method = method;
        Handler = handler;
    }

    public bool Matches(RequestContext context)
    {
        return context.path.StartsWith(Path) &&
               context.method.Equals(Method, StringComparison.OrdinalIgnoreCase);
    }
}