using System.Text;

namespace web_app_scratch;

public class HttpBodyParser
{
    public static string Parse(byte[] rawBody)
    {
        if (rawBody.Length == 0)
        {
            return string.Empty;
        }
        return Encoding.UTF8.GetString(rawBody);
    }
}
