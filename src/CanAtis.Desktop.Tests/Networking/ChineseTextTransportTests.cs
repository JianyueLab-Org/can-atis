using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Can.Network;
using Can.Network.PDU;
using CanAtis.Networking;
using Xunit;

namespace CanAtis.Desktop.Tests.Networking;

public class ChineseTextTransportTests
{
    [Fact]
    public void FormatAtisText_SplitsLongChineseTextWithoutLosingCharacters()
    {
        var text = string.Concat(Enumerable.Repeat("上海浦东机场通播信息阿尔法使用跑道三五左落地", 6));

        var lines = NetworkConnection.FormatAtisText(text);

        Assert.True(lines.Count > 1);
        Assert.All(lines, line => Assert.True(line.Length <= 64));
        Assert.Equal(text, string.Concat(lines));
    }

    [Fact]
    public void FormatAtisText_StillBreaksEnglishAtSpaces()
    {
        var text = string.Join(' ', Enumerable.Repeat("INFORMATION ALPHA", 10));

        var lines = NetworkConnection.FormatAtisText(text);

        Assert.All(lines, line => Assert.True(line.Length <= 64));
        Assert.All(lines, line => Assert.DoesNotContain("  ", line));
        Assert.Equal(text, string.Join(' ', lines));
    }

    [Fact]
    public async Task FsdSession_SendsAndReceivesChineseAsUtf8()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var received = new BlockingCollection<string>();
        var session = new FsdSession(new ClientProperties("test", new Version(1, 0)));
        session.RawDataReceived += (_, e) => received.Add(e.Data);

        session.Connect("127.0.0.1", port);
        using var server = await listener.AcceptTcpClientAsync();
        var stream = server.GetStream();
        await WaitUntil(() => session.Connected);

        // Client -> server: Chinese text must arrive as UTF-8.
        session.SendPdu(new PDUTextMessage("ZSPD_ATIS", "@19400", "浦东通播 A 跑道35L"));
        var buffer = new byte[1024];
        var sb = new StringBuilder();
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            while (!sb.ToString().Contains("\r\n"))
            {
                var n = await stream.ReadAsync(buffer, cts.Token);
                sb.Append(Encoding.UTF8.GetString(buffer, 0, n));
            }
        }

        Assert.Contains("浦东通播 A 跑道35L", sb.ToString());

        // Server -> client: a multibyte character split across two TCP writes must still decode.
        var packet = Encoding.UTF8.GetBytes("#TMSERVER:ZSPD_ATIS:中文测试\r\n");
        var split = Array.IndexOf(packet, (byte)':', 10) + 2; // inside the first Chinese character
        await stream.WriteAsync(packet.AsMemory(0, split));
        await stream.FlushAsync();
        await Task.Delay(100);
        await stream.WriteAsync(packet.AsMemory(split));
        await stream.FlushAsync();

        Assert.True(received.TryTake(out var raw, TimeSpan.FromSeconds(5)));
        Assert.Contains("中文测试", raw);

        session.Disconnect();
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(50);

        Assert.True(condition());
    }
}
