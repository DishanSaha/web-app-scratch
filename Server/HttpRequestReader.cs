using System.Net.Sockets;
using System.Text;

namespace web_app_scratch;

public class HttpRequestReader
{
    private const int MaxHeaderSize = 32 * 1024;
    private const int MaxBodySize = 10 * 1024 * 1024;

    public static async Task<(byte[] Header, byte[] Body)> ReadAsync(NetworkStream stream)
    {
        using var memoryBuffer = new MemoryStream();
        var tempBuffer = new byte[4096];
        var headerEnd = -1;

        // Read header----
        while (true)
        {
            var read = await stream.ReadAsync(tempBuffer);
            if (read == 0)
            {
                throw new EndOfStreamException("Client disconnected before completing request");
            }
            memoryBuffer.Write(tempBuffer, 0, read);
            var raw = memoryBuffer.ToArray();

            // \r\n\r\n--
            for (var i = 0; i < raw.Length - 3; i++)
            {
                if (raw[i] == '\r' && raw[i + 1] == '\n' && raw[i + 2] == '\r' && raw[i + 3] == '\n')
                {
                    headerEnd = i + 4;
                    break;
                }
            }
            if (headerEnd != -1) break;

            // Header limit
            if (memoryBuffer.Length > MaxHeaderSize) throw new HttpException(413, "Request header too large");
        }
        var headerBytes = memoryBuffer.ToArray()[..headerEnd];
        var headerText = Encoding.UTF8.GetString(headerBytes);
        var contentLength = 0;
        foreach (var line in headerText.Split("\r\n"))
        {
            if (line.StartsWith("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                var value = line["Content-Length:".Length..].Trim();
                int.TryParse(value, out contentLength);
                break;
            }
        }

        // Read Body-----
        
        byte[] bodyBytes = [];
        if (contentLength > 0)
        {
            if (contentLength > MaxBodySize)
            {
                throw new HttpException(413, "Request Body too large");
            }
            using var bodyBuffer = new MemoryStream();
            var alreadyRead = (int)memoryBuffer.Length - headerEnd;
            if (alreadyRead > 0)
            {
                bodyBuffer.Write(memoryBuffer.ToArray(), headerEnd, alreadyRead);
            }
            var total = alreadyRead;
            while (total < contentLength)
            {
                var n = await stream.ReadAsync(tempBuffer, 0, Math.Min(tempBuffer.Length, contentLength - total));
                if (n == 0)
                {
                    throw new EndOfStreamException("Client disconnected during body request");
                }
                bodyBuffer.Write(tempBuffer, n, 0);
                total += n;
            }
            bodyBytes = bodyBuffer.ToArray();
        }
        return (headerBytes, bodyBytes);
    }
}