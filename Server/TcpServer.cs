using System.Net;
using System.Net.Sockets;
using System.Text;
using web_app_scratch.Core;

namespace web_app_scratch;

public class TcpServer
{
    private readonly int _port;
    private readonly Func<RequestContext, Task> _pipeline;
    public TcpServer(int port, Func<RequestContext, Task> pipeline)
    {
        _port = port;
        _pipeline = pipeline;
    }
    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"Listening on{((IPEndPoint)listener.LocalEndpoint).Port}");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            await HandleClient(client);
        }
    }
    private async Task HandleClient(TcpClient client)
    {
        await using var stream = client.GetStream();

        var (rawHeader, rawBody) = await HttpRequestReader.ReadAsync(stream);
        var context = HttpHeaderParser.Parse(rawHeader);
        context.Body = HttpBodyParser.Parse(rawBody);

        // Router-এর বদলে এখন pipeline call করি
        await _pipeline(context);
        var response = context.Response ?? "Ok";

        var responseInByte = Encoding.UTF8.GetBytes(
            "HTTP/1.1 200 OK \r\n" +
            "Content-Length: " + response.Length + "\r\n" +
            "X-Name : Mredul\r\n\r\n" +
            response
        );
        await stream.WriteAsync((ReadOnlyMemory<byte>)responseInByte);

    }
}
