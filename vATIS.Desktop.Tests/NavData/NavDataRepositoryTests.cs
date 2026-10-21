using System;
using System.IO;
using System.Threading.Tasks;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Xunit;

namespace Vatsim.Vatis.Tests.NavData;

[Collection("AppData")]
public sealed class NavDataRepositoryTests : IDisposable
{
    private readonly string _appDataPath = Path.Combine(Path.GetTempPath(), "vatis-tests-" + Guid.NewGuid());

    public NavDataRepositoryTests()
    {
        // An empty app data folder has no downloaded Airports.json, so Initialize() falls back to the bundled file.
        PathProvider.SetAppDataPath(_appDataPath);
    }

    public void Dispose()
    {
        PathProvider.SetAppDataPath(string.Empty);
        if (Directory.Exists(_appDataPath))
        {
            Directory.Delete(_appDataPath, recursive: true);
        }
    }

    [Fact]
    public void BundledAirports_AreCopiedToOutput()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "airports.json");
        Assert.True(File.Exists(path), "Bundled airport data must be copied to the output directory.");
    }

    [Theory]
    [InlineData("ZBAA")]
    [InlineData("ZSPD")]
    [InlineData("ZGGG")]
    [InlineData("ZUUU")]
    [InlineData("VHHH")]
    [InlineData("RCTP")]
    [InlineData("RJTT")]
    // These were in the CAN airport list but not in the vATIS one; a station at any of them must still load.
    [InlineData("ZBDT")]
    [InlineData("ZLDH")]
    [InlineData("ZYDQ")]
    [InlineData("ZUYB")]
    [InlineData("ZWKM")]
    [InlineData("ZYMD")]
    [InlineData("ZSJJ")]
    public async Task Initialize_WithoutDownloadedNavData_ResolvesCanAirports(string id)
    {
        var repository = new NavDataRepository(null!, null!);
        await repository.Initialize();

        var airport = repository.GetAirport(id);

        Assert.NotNull(airport);
        Assert.False(string.IsNullOrEmpty(airport.Name));
        Assert.InRange(airport.Latitude, -90, 90);
        Assert.InRange(airport.Longitude, -180, 180);
    }

    [Fact]
    public async Task Initialize_WithoutDownloadedNavData_KeepsAirportNames()
    {
        var repository = new NavDataRepository(null!, null!);
        await repository.Initialize();

        var airport = repository.GetAirport("ZBAA");

        Assert.NotNull(airport);
        Assert.Equal("BEIJING CAPITAL INTERNATION AIRPORT", airport.Name);
        Assert.Equal(40.0725, airport.Latitude, 4);
        Assert.Equal(116.5975, airport.Longitude, 4);
    }
}
