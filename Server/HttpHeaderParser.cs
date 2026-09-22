namespace web_app_scratch;

using System.Text;
public class HttpHeaderParser
{
    public static RequestContext Parse(byte[] rawHeader)
    {
        var text = Encoding.UTF8.GetString(rawHeader);
        var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var requestLine = lines[0].Split(' ');
        var ctx = new RequestContext
        {
            method = requestLine[0],
            path = requestLine[1],
            version = requestLine.Length > 2 ? requestLine[2] : "HTTP/1.1"
        };

        for (var i = 1; i < lines.Length; i++)
        {
            var colon = lines[i].IndexOf(':');
            if (colon >= 0)
            {
                var key = lines[i][..colon].Trim();
                var value = lines[i][(colon + 1)..].Trim();
                ctx.Headers[key] = value;
            }
        }
        return ctx;
    }
}

// client send--

// POST /login HTTP/1.1
// Host: localhost:8080
// Content-Type: application/json
// Content-Length: 25

// after parser---

// RequestContext
// │
// ├── method  → "POST"
// ├── path    → "/login"
// ├── version → "HTTP/1.1"
// │
// └── Headers
//     ├── Host → "localhost:8080"
//     ├── Content-Type → "application/json"
//     └── Content-Length → "25"