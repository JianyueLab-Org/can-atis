using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CanAtis.Atis;
using CanAtis.Config;
using CanAtis.Profiles.Models;
using CanAtis.TextToSpeech;
using Xunit;

namespace CanAtis.Desktop.Tests.TextToSpeech;

public class TextToSpeechServiceTests
{
    [Fact]
    public void PrepareText_KeepsAsciiAndReplacesBrackets()
    {
        var text = TextToSpeechService.PrepareText("  INFORMATION ALPHA [:nh] WIND 090 AT 5.\r\n ");
        Assert.Equal("INFORMATION ALPHA  :nh  WIND 090 AT 5.", text);
    }

    [Fact]
    public void PrepareText_StripsAccentsAndNormalizesSpaces()
    {
        Assert.Equal("CAFE ONE", TextToSpeechService.PrepareText("CAFÉ ONE"));
    }

    [Fact]
    public void PrepareText_RejectsChinese()
    {
        Assert.Throws<AtisBuilderException>(() => TextToSpeechService.PrepareText("广州 INFORMATION ALPHA"));
    }

    [Fact]
    public async Task RequestAudio_PostsCanTtsRequestAndReturnsPcm()
    {
        using var server = new FakeTtsServer();
        string? body = null;
        var serve = server.Serve(async ctx =>
        {
            // Voices request.
            ctx.Response.ContentType = "application/json";
            await Write(ctx, Encoding.UTF8.GetBytes("""[{"name":"Huge Harry","id":"huge-harry"}]"""));
        }, async ctx =>
        {
            using var reader = new StreamReader(ctx.Request.InputStream);
            body = await reader.ReadToEndAsync();
            ctx.Response.ContentType = "application/octet-stream";
            await Write(ctx, [1, 0, 2, 0]);
        });

        var service = server.CreateService(new AppConfig { UserId = "1234", PasswordDecrypted = "secret" });
        await service.Initialize();
        Assert.Equal("huge-harry", Assert.Single(service.VoiceList).Id);

        var station = new AtisStation { Identifier = "ZGGG", Name = "Test" };
        station.AtisVoice.Voice = "Huge Harry";
        station.AtisVoice.SpeechRate = 200;
        var audio = await service.RequestAudio("GUANGZHOU INFORMATION ALPHA.", station, 'A', CancellationToken.None);
        await serve;

        Assert.Equal(new byte[] { 1, 0, 2, 0 }, audio);
        using var doc = JsonDocument.Parse(body!);
        var root = doc.RootElement;
        Assert.Equal("GUANGZHOU INFORMATION ALPHA.", root.GetProperty("text").GetString());
        Assert.Equal("huge-harry", root.GetProperty("voice").GetString());
        Assert.Equal(200, root.GetProperty("speechRate").GetInt32());
        Assert.Equal("1234", root.GetProperty("cid").GetString());
        Assert.Equal("secret", root.GetProperty("password").GetString());
    }

    [Fact]
    public async Task RequestAudio_ReportsRejectedCredentials()
    {
        using var server = new FakeTtsServer();
        var serve = server.Serve(async ctx =>
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/json";
            await Write(ctx, Encoding.UTF8.GetBytes("""{"error":"Invalid CAN credentials"}"""));
        });

        var service = server.CreateService(new AppConfig { UserId = "1234", PasswordDecrypted = "wrong" });
        var ex = await Assert.ThrowsAsync<AtisBuilderException>(() => service.RequestAudio("WIND CALM.",
            new AtisStation { Identifier = "ZGGG", Name = "Test" }, 'A', CancellationToken.None));
        await serve;
        Assert.Contains("CAN ID", ex.Message);
    }

    [Fact]
    public async Task RequestAudio_RequiresCredentials()
    {
        var service = new TextToSpeechService(new AppConfig());
        await Assert.ThrowsAsync<AtisBuilderException>(() => service.RequestAudio("WIND CALM.",
            new AtisStation { Identifier = "ZGGG", Name = "Test" }, 'A', CancellationToken.None));
    }

    private static async Task Write(HttpListenerContext ctx, byte[] bytes)
    {
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }

    private sealed class FakeTtsServer : IDisposable
    {
        private static readonly object s_envLock = new();
        private readonly HttpListener _listener = new();
        private readonly string _baseUrl;

        public FakeTtsServer()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            _baseUrl = $"http://localhost:{port}/";
            _listener.Prefixes.Add(_baseUrl);
            _listener.Start();
        }

        public TextToSpeechService CreateService(IAppConfig config)
        {
            lock (s_envLock)
            {
                Environment.SetEnvironmentVariable("CAN_TTS_URL", _baseUrl + "speech/synthesize");
                Environment.SetEnvironmentVariable("CAN_TTS_VOICES_URL", _baseUrl + "speech/voices");
                try
                {
                    return new TextToSpeechService(config);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("CAN_TTS_URL", null);
                    Environment.SetEnvironmentVariable("CAN_TTS_VOICES_URL", null);
                }
            }
        }

        public async Task Serve(params Func<HttpListenerContext, Task>[] handlers)
        {
            foreach (var handler in handlers)
                await handler(await _listener.GetContextAsync());
        }

        public void Dispose() => _listener.Close();
    }
}
