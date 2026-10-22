using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CanAtis.Profiles.Models;
using CanAtis.TextToSpeech;
using Xunit;

namespace CanAtis.Tests.Voice;

public class WindowsTextToSpeechTests
{
    [Fact]
    public async Task WindowsSapi_GeneratesNonSilentPcm()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var service = new TextToSpeechService();
        await service.Initialize();
        Assert.NotEmpty(service.VoiceList);
        var audio = await service.RequestAudio("Airport information alpha. Wind calm.",
            new AtisStation { Identifier = "TEST", Name = "Test" }, 'A', CancellationToken.None);
        Assert.NotNull(audio);
        Assert.True(audio.Length > 9600);
        Assert.Equal(0, audio.Length % 2);
        Assert.True(audio.Any(sample => sample != 0));
    }
}
