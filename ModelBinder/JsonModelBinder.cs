using System.Text.Json;

namespace web_app_scratch.ModelBinder;

public class JsonModelBinder
{
    public bool CanBind(RequestContext context)
    {
        if (string.IsNullOrEmpty(context.Body))
        {
            return false;
        }
        if (!context.Headers.TryGetValue("Content-Type", out var ct))
        {
            return false;
        }
        return ct.Contains("application/json");
    }

    public object? Bind(RequestContext context, Type targetType)
    {
        try
        {
            return JsonSerializer.Deserialize(context.Body, targetType);
        }
        catch (JsonException)
        {
            throw new HttpException(400, $"Invalid json type for '{targetType.Name}");
        }
    }
}