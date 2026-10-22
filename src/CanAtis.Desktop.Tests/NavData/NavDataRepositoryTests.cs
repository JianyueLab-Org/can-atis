using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CanAtis.NavData;
using Xunit;

namespace CanAtis.Tests.NavData;

public class NavDataRepositoryTests
{
    [Fact]
    public async Task LoadBundledAirports_ReadsOfficialAirportArray()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "airports.json");
        Assert.True(File.Exists(path), "Bundled airport data must be copied to the output directory.");

        var method = typeof(NavDataRepository).GetMethod(
            "LoadBundledAirports", BindingFlags.NonPublic | BindingFlags.Static)!;
        var airports = await (Task<List<Airport>>)method.Invoke(null, [path])!;

        Assert.NotEmpty(airports);
        var airport = Assert.Single(airports, airport => airport.Id == "01ID");
        Assert.Equal("LAVA HOT SPRINGS AIRPORT", airport.Name);
        Assert.Equal(42.60825, airport.Latitude, 6);
        Assert.Equal(-112.032461, airport.Longitude, 6);
    }
}
