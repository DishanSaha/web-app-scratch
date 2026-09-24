namespace web_app_scratch.Core;

using web_app_scratch.ModelBinder;

public class Router
{
    private readonly List<Endpoint> _endpoints = [];
    private readonly HandlerInvoker _invoker;

    public Router(HandlerInvoker invoker)
    {
        _invoker = invoker;
    }


    // Lambda registration 
    public Endpoint MapGet(string path, Delegate handler)
    {
        // Console.WriteLine($"[Router] MapGet called: GET {path}");
        var endpoint = new Endpoint(path, "GET", handler);
        _endpoints.Add(endpoint);
        // Console.WriteLine($"[Router] total endpoints: {_endpoints.Count}");
        return endpoint;
    }
    public Endpoint MapPost(string path, Delegate handler)
    {
        var endpoint = new Endpoint(path, "POST", handler);
        _endpoints.Add(endpoint);
        return endpoint;
    }
    // Controller registration
    public void RegisterControllers(params Type[] controllerTypes)
    {
        var endpoints = ControllerDiscovery.Discover(controllerTypes);
        _endpoints.AddRange(endpoints);
    }
    public string Resolve(RequestContext context)
    {
        // Console.WriteLine($"[Router] Resolve: {context.method} {context.path}");
        // Console.WriteLine($"[Router] endpoints count: {_endpoints.Count}");

        var endpoint = _endpoints.FirstOrDefault(ep => ep.Matches(context));
        if (endpoint is null) return "404 not found";

        return _invoker.InvokeMethod(
            endpoint.ActionMethod,
            endpoint.Target,
            context
        );
    }
}

// Router = Route register করা + incoming request
// -এর জন্য matching Endpoint খুঁজে বের করা + handler execute করানো।


// ******/ Lamda/Delegate based--*****
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




// #### Controller Based #######---
        //             ROUTE REGISTRATION
        //                    │
        //      ┌─────────────┴─────────────┐
        //      │                           │
        // MapGet/MapPost             RegisterControllers
        //      │                           │
        //   Delegate              ControllerDiscovery
        //      │                           │
        //      └─────────────┬─────────────┘
        //                    ↓
        //               List<Endpoint>
        //                    │
        //                    │
        //             Incoming Request
        //                    ↓
        //                 Router
        //                    ↓
        //              Resolve(context)
        //                    ↓
        //            Endpoint.Matches()
        //                    │
        //             ┌──────┴──────┐
        //             │             │
        //           Match         No match
        //             │             │
        //             ↓             ↓
        //       HandlerInvoker    404
        //             │
        //             ↓
        //        Execute Method