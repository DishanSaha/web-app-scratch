namespace web_app_scratch.Core;

public class Router
{
    private readonly List<Endpoint> _endpoints = [];

    public Endpoint MapGet(string path, Delegate handler)
    {
        Console.WriteLine($"[Router] MapGet called: GET {path}");
        var endpoint = new Endpoint(path, "GET", handler);
        _endpoints.Add(endpoint);
        Console.WriteLine($"[Router] total endpoints: {_endpoints.Count}");
        return endpoint;
    }

    public string Resolve(RequestContext context)
    {
        Console.WriteLine($"[Router] Resolve: {context.method} {context.path}");
        Console.WriteLine($"[Router] endpoints count: {_endpoints.Count}");

        var endpoint = _endpoints.FirstOrDefault(ep => ep.Matches(context));
        if (endpoint is null)
        {
            Console.WriteLine("[Router] NO MATCH -> 404");
            return "404 not found";
        }

        Console.WriteLine("[Router] MATCHED!");
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