public class HttpException : Exception
{
    public int statusCode { get; }
    public HttpException(int code, string message) : base(message)
    => statusCode = code;
}