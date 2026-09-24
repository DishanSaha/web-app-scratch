using System.Reflection;

namespace web_app_scratch.Core;

public class Endpoint
{
    public string Path { get; }
    public string HttpMethod { get; }
    public MethodInfo ActionMethod { get; }
    public object? Target { get; }

    // Constructor 1: Delegate-based (lambda / MapGet style)
    public Endpoint(string path, string method, Delegate handler)
    {
        Path = path;
        HttpMethod = method;
        ActionMethod = handler.Method;
        Target = handler.Target;
    }

    // Constructor 2: Controller-based (MethodInfo + instance)
    public Endpoint(string path, string method, MethodInfo methodInfo, object target)
    {
        Path = path;
        HttpMethod = method;
        ActionMethod = methodInfo;
        Target = target;
    }

    public bool Matches(RequestContext context)
    {
        return context.path.StartsWith(Path) &&
               context.method.Equals(HttpMethod, StringComparison.OrdinalIgnoreCase);
    }
}