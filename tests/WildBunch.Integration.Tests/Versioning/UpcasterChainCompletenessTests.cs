using System.Reflection;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Integration.Tests.Versioning;

/// <summary>
/// Build-time test: asserts every IEventUpcaster in the assembly is registered
/// in the DI registration call. No silent missed upcasters.
/// See the event sourcing integrity policy.
/// </summary>
public sealed class UpcasterChainCompletenessTests
{
    [Fact]
    public void AllEventUpcastersInAssembly_AreRegisteredInDi()
    {
        // The DI registration in DependencyInjection.cs explicitly lists upcasters.
        // This test asserts that every IEventUpcaster class in the WildBunch.Persistence
        // assembly is referenced by that registration.
        //
        // Since no upcasters exist yet, this test asserts that the assembly contains
        // zero IEventUpcaster implementations. When the first upcaster is written,
        // this test will fail until it's registered in DependencyInjection.cs.

        var upcasterType = typeof(IEventUpcaster);
        var assembly = typeof(PayloadUpcasterRegistry).Assembly;

        var allUpcasters = assembly.GetTypes()
            .Where(t => upcasterType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        // No upcasters exist yet. When upcasters are added, the DI registration
        // in DependencyInjection.cs must reference them. This test will need to
        // be updated to verify the registration list matches the assembly scan.
        Assert.Empty(allUpcasters);
    }

    [Fact]
    public void Registry_WithNoUpcasters_ReturnsVersion1ForAllTypes()
    {
        var registry = new PayloadUpcasterRegistry([]);

        Assert.Equal(1, registry.CurrentVersion("GameStarted"));
        Assert.Equal(1, registry.CurrentVersion("TravelDayAdvanced"));
        Assert.Equal(1, registry.CurrentVersion("StoreItemPurchased"));
    }

    [Fact]
    public void Registry_WithNoUpcasters_UpcastReturnsPayloadUnchanged()
    {
        var registry = new PayloadUpcasterRegistry([]);

        var json = """{"test":"value"}""";
        var result = registry.Upcast("GameStarted", storedVersion: 1, json);
        Assert.Equal(json, result);
    }

    [Fact]
    public void Registry_FutureVersion_Throws()
    {
        var registry = new PayloadUpcasterRegistry([]);

        Assert.Throws<InvalidOperationException>(() =>
            registry.Upcast("GameStarted", storedVersion: 2, """{"test":"value"}"""));
    }
}
