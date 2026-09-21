namespace web_app_scratch.Core;

public class Router
{
    private readonly List<EndPoint> _endpoints = [];

    public EndPoint MapGet(string path, Delegate handler)
    {
        var endpoint = new EndPoint(path, "GET", handler);
        _endpoints.Add(endpoint);
        return endpoint;
    }

    public string Resolve(RequestContext context)
    {
        var endpoint = _endpoints.FirstOrDefault(ep => ep.Matches(context));
        if (endpoint is null) return "404 not found";

        var method = endpoint.Handler.Method;
        var args = new object?[1];
        args[0] = context;
        var result = method.Invoke(endpoint.Handler.Target, args);
        return result?.ToString() ?? "";
    }
}

// RequestContext
//       │
//       ▼
//  _endpoints
//       │
//       ▼
// FirstOrDefault()
//       │
//       ├── Endpoint 1 → Matches? ❌
//       │
//       ├── Endpoint 2 → Matches? ❌
//       │
//       └── Endpoint 3 → Matches? ✅
//                          │
//                          ▼
//                       Handler
//                          │
//                          ▼
//                        Method
//                          │
//                          ▼
//                       Invoke()
//                          │
//                          ▼
//                        Result
//                          │
//                          ▼
//                      string

// 1. MapGet()
//       ↓
//    Endpoint register করে

// 2. Resolve()
//       ↓
//    কোন Endpoint match করে সেটা খুঁজে

// 3. Invoke()
//       ↓
//    সেই Endpoint-এর Handler execute করে