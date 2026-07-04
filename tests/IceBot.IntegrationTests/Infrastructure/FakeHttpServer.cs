using System.Net;
using System.Net.Sockets;

namespace IceBot.IntegrationTests.Infrastructure;

internal sealed class FakeHttpServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Func<int, HttpListenerContext, Task> _handler;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _loop;
    private int _requestCount;

    public FakeHttpServer(Func<int, HttpListenerContext, Task> handler)
    {
        _handler = handler;
        var port = GetAvailablePort();
        BaseAddress = new Uri($"http://127.0.0.1:{port}/");
        _listener.Prefixes.Add(BaseAddress.ToString());
        _listener.Start();
        _loop = RunAsync();
    }

    public Uri BaseAddress { get; }
    public int RequestCount => Volatile.Read(ref _requestCount);

    private async Task RunAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(_shutdown.Token);
                var attempt = Interlocked.Increment(ref _requestCount);
                try
                {
                    await _handler(attempt, context);
                    context.Response.Close();
                }
                catch (Exception ex) when (
                    ex is HttpListenerException or IOException or ObjectDisposedException)
                {
                    // A cancelled HTTP client may disconnect before the fake server writes its response.
                }
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                return;
            }
            catch (HttpListenerException) when (_shutdown.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        await _shutdown.CancelAsync();
        _listener.Stop();
        await _loop;
        _listener.Close();
        _shutdown.Dispose();
    }
}
