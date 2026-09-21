namespace web_app_scratch.Core;

public class EndPoint(string method, string path, Delegate handler)
{
    private readonly string Path = path;
    private readonly string Method = method;
    public readonly Delegate Handler = handler;

    public bool Matches(RequestContext context)
    {
        return context.path.StartsWith(path) &&
        context.method.Equals(Method, StringComparison.OrdinalIgnoreCase);
    }
}