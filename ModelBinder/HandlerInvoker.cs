using System.Reflection;
using web_app_scratch.DI;

namespace web_app_scratch.ModelBinder;

public class HandlerInvoker(ServiceProvider services)
{
    private readonly JsonModelBinder _modelBinder = new();

    public string InvokeMethod(MethodInfo method, object? target, RequestContext context)
    {
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.ParameterType == typeof(RequestContext))
            {
                args[i] = context;
                continue;
            }
            // DI থেকে resolve করার চেষ্টা
            try
            {
                var service = services.GetService(p.ParameterType);
                if (services != null)
                {
                    args[i] = services;
                    continue;
                }
            }
            catch
            {
                // service register হয়নি — এটা throw করে, এটাই expected
            }
            // Body থেকে bind করার চেষ্টা
            if (_modelBinder.CanBind(context))
            {
                args[i] = _modelBinder.Bind(context, p.ParameterType);
                continue;
            }
            args[i] = null;
        }
        // Handler invoke
        try
        {
            var result = method.Invoke(target, args);
            return result?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandlerInvoker] handler threw: {ex.Message}");
            return "500 Internal Server Error";
        }
    }
}


