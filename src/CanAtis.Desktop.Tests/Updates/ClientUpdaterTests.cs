using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CanAtis.Config;
using CanAtis.Io;
using CanAtis.Updates;
using Xunit;

namespace CanAtis.Tests.Updates;

public class ClientUpdaterTests
{
    [Fact]
    public async Task PortableClientChecksJsonAndOffersRelayWithoutInstalling()
    {
        var (update, request) = await Check("""
            {"client":"atis-for-can","apps":{"atis-for-can":{"version":"99.1.2","download":"https://github.com/example/unused.zip"}},"update":{"available":true,"latest":"99.1.2"}}
            """);

        Assert.NotNull(update);
        Assert.Equal("99.1.2", update.Version);
        Assert.Equal("https://api.ceruleanavi.net/api/v1/clients/download/atis-for-can?v=99.1.2", update.DownloadUrl);
        var current = typeof(ClientUpdater).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Equal($"/api/v1/clients/latest?client=atis-for-can&version={Uri.EscapeDataString(current)}", request);
    }

    [Theory]
    [InlineData("{\"update\":{\"available\":false,\"latest\":\"99.1.2\"}}")]
    [InlineData("{\"update\":{\"available\":true,\"latest\":\"2.2.36\"}}")]
    [InlineData("{\"update\":{\"available\":true,\"latest\":\"\"}}")]
    [InlineData("{\"version\":\"99.1.2\"}")]
    [InlineData("{\"update\":{\"available\":\"true\",\"latest\":\"99.1.2\"}}")]
    [InlineData("not JSON")]
    public async Task NoUpdateOrInvalidResponseDoesNotOfferDownload(string json)
    {
        var (update, _) = await Check(json);
        Assert.Null(update);
    }

    [Fact]
    public async Task UnavailableServiceDoesNotBlockStartup()
    {
        var (update, _) = await Check("{}", 503);
        Assert.Null(update);
    }

    private static async Task<(ClientUpdate? Update, string Request)> Check(string json, int status = 200)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var serve = Task.Run(async () =>
            {
                using var connection = await listener.AcceptTcpClientAsync();
                await using var stream = connection.GetStream();
                using var reader = new System.IO.StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                var request = await reader.ReadLineAsync();
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
                var body = Encoding.UTF8.GetBytes(json);
                var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Response\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(headers);
                await stream.WriteAsync(body);
                return request!.Split(' ')[1];
            });
            var updater = new ClientUpdater(new Configuration($"http://127.0.0.1:{port}/api/v1/clients/latest?client=atis-for-can"), new Downloader());
            var update = await updater.Run();
            return (update, await serve.WaitAsync(TimeSpan.FromSeconds(15)));
        }
        finally
        {
            listener.Stop();
        }
    }

    private sealed class Configuration(string versionUrl) : IAppConfigurationProvider
    {
        public string VersionUrl => versionUrl;
        public string MetarUrl => string.Empty;
        public string NavDataUrl => string.Empty;
        public string AtisHubUrl => string.Empty;
        public string VoiceListUrl => string.Empty;
        public string TextToSpeechUrl => string.Empty;
        public string DigitalAtisApiUrl => string.Empty;
        public string FsdHost => string.Empty;
        public int FsdPort => 6809;
        public string AudioHost => string.Empty;
        public int AudioPort => 64738;
        public Task Initialize() => Task.CompletedTask;
    }
}
