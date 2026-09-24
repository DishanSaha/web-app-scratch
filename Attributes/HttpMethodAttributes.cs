namespace web_app_scratch.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public abstract class HttpMethodAttribute : Attribute
{
    public string Path { get; }
    public string Method { get; }

    protected HttpMethodAttribute(string method, string path)
    {
        Method = method;
        Path = path;
    }
}

public sealed class HttpGetAttribute : HttpMethodAttribute
{
    public HttpGetAttribute(string path) : base("GET", path) { }
}
public sealed class HttpPostAttribute : HttpMethodAttribute
{
    public HttpPostAttribute(string path) : base("POST", path) { }
}