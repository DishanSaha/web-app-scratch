using System.Reflection;
using web_app_scratch.Attributes;

namespace web_app_scratch.Core;

public static class ControllerDiscovery
{
    public static List<Endpoint> Discover(params Type[] controllerTypes)
    {
        var endpoints = new List<Endpoint>();

        foreach (var controllerType in controllerTypes)
        {
            var instance = Activator.CreateInstance(controllerType)
            ?? throw new InvalidOperationException($"Cannot create instance of{controllerType.Name}");

            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<HttpMethodAttribute>();
                if (attr is null) continue;

                endpoints.Add(new Endpoint(attr.Path, attr.Method, method, instance));
            }
        }
        return endpoints;
    }
}


// attr.Path
//     ↓
// "/users"

// attr.Method
//     ↓
// "GET"

// method
//     ↓
// GetUsers() MethodInfo

// instance
//     ↓
// UserController object



// Endpoint
// ├── Path = "/users"
// ├── HttpMethod = "GET"
// ├── ActionMethod = GetUsers()
// └── Target = UserController instance



    //           UserController
    //                  ↓
    //       Activator.CreateInstance
    //                  ↓
    //        UserController object
    //                  ↓
    //          GetMethods()
    //                  ↓
    //     ┌────────────┼─────────────┐
    //     ↓            ↓             ↓
    // GetUsers    CreateUser    SomethingElse
    //     ↓            ↓             ↓
    // [HttpGet]     [HttpPost]      no attribute
    //     ↓            ↓             ↓
    // Endpoint      Endpoint       skip