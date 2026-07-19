using System.Reflection;
using WildBunch.Persistence;
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
        // Scan the assembly for all concrete IEventUpcaster implementations.
        var interfaceType = typeof(IEventUpcaster);
        var assembly = typeof(PayloadUpcasterRegistry).Assembly;

        var allUpcastersInAssembly = assembly.GetTypes()
            .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        // Call the same method that DI uses to build the registry.
        var registeredUpcasters = DependencyInjection.CreateDefaultUpcasters();

        // Every upcaster in the assembly must appear in the DI registration list.
        // This catches the case where a new upcaster class is added to the assembly
        // but forgotten in CreateDefaultUpcasters().
        var registeredTypes = registeredUpcasters.Select(u => u.GetType()).ToHashSet();
        foreach (var upcasterTypeInAssembly in allUpcastersInAssembly)
        {
            Assert.Contains(upcasterTypeInAssembly, registeredTypes);
        }

        // If there are upcasters, verify the registry accepts them (chain validation).
        if (allUpcastersInAssembly.Count > 0)
        {
            var registry = new PayloadUpcasterRegistry(registeredUpcasters);
            Assert.NotEmpty(registry.RegisteredPayloadTypes);
        }
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

    [Fact]
    public void Registry_UnknownTypeWithStoredVersionNot1_Throws()
    {
        var registry = new PayloadUpcasterRegistry([]);

        Assert.Throws<InvalidOperationException>(() =>
            registry.Upcast("UnknownEventType", storedVersion: 2, """{"test":"value"}"""));
    }
}
