using System.Reflection;
using WildBunch.Domain.Events;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

// Required for IsInitOnly reflection on setter return type modifiers.
using System.Runtime.CompilerServices;

namespace WildBunch.Domain.Tests.Events;

public class TypedDomainEventTests
{
    private static readonly string[] EnvelopeFieldNames =
    [
        "EventId",
        "Sequence",
        "OccurredAtUtc",
        "SchemaVersion",
        "CorrelationId",
        "CausationId",
        "StreamId"
    ];

    [Fact]
    public void GameStarted_Implements_IDomainEvent()
    {
        var e = NewGameStarted();
        Assert.IsAssignableFrom<IDomainEvent>(e);
    }

    [Fact]
    public void StoreItemPurchased_Implements_IDomainEvent()
    {
        var e = NewStoreItemPurchased();
        Assert.IsAssignableFrom<IDomainEvent>(e);
    }

    [Fact]
    public void GameStarted_All_Required_Fields_Are_Settable_Via_Init()
    {
        var e = NewGameStarted();
        Assert.Equal("Doc", e.PlayerName);
        Assert.Equal(new TownId("town-1"), e.StartingTownId);
        Assert.Equal("Dodge City", e.StartingTownName);
        Assert.Equal(1000, e.StartingHealth);
        Assert.Equal(25m, e.StartingWallet);
        Assert.Equal(GameDifficulty.Standard, e.Difficulty);
        Assert.Equal(TravelRandomnessState.CreateDeterministic("test-salt").Mode, e.TravelRandomness.Mode);
        Assert.Equal(GameEntropy.Classic, e.Entropy);
    }

    [Fact]
    public void StoreItemPurchased_All_Required_Fields_Are_Settable_Via_Init()
    {
        var e = NewStoreItemPurchased();
        Assert.Equal(new TownId("town-1"), e.TownId);
        Assert.Equal(ItemKind.RevolverAmmo, e.ItemKind);
        Assert.Equal("revolver ammo", e.DisplayName);
        Assert.Equal(3, e.Quantity);
        Assert.Equal(2m, e.UnitPrice);
        Assert.Equal(6m, e.TotalPrice);
        Assert.Equal(19m, e.WalletAfter);
    }

    [Fact]
    public void GameStarted_Properties_Are_Init_Only()
    {
        var properties = typeof(GameStarted).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: true);
            Assert.NotNull(setter);
            // init-only setters have the modreq(IsExternalInit) on the return type.
            var returnTypeModifiers = setter.ReturnParameter.GetRequiredCustomModifiers();
            Assert.Contains(typeof(IsExternalInit), returnTypeModifiers);
        }
    }

    [Fact]
    public void StoreItemPurchased_Properties_Are_Init_Only()
    {
        var properties = typeof(StoreItemPurchased).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: true);
            Assert.NotNull(setter);
            var returnTypeModifiers = setter.ReturnParameter.GetRequiredCustomModifiers();
            Assert.Contains(typeof(IsExternalInit), returnTypeModifiers);
        }
    }

    [Fact]
    public void GameStarted_Has_Value_Equality()
    {
        var a = NewGameStarted();
        var b = NewGameStarted();
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void StoreItemPurchased_Has_Value_Equality()
    {
        var a = NewStoreItemPurchased();
        var b = NewStoreItemPurchased();
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GameStarted_Does_Not_Carry_Envelope_Fields()
    {
        var properties = typeof(GameStarted).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var names = properties.Select(p => p.Name).ToList();
        foreach (var envelopeField in EnvelopeFieldNames)
        {
            Assert.DoesNotContain(envelopeField, names);
        }
    }

    [Fact]
    public void StoreItemPurchased_Does_Not_Carry_Envelope_Fields()
    {
        var properties = typeof(StoreItemPurchased).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var names = properties.Select(p => p.Name).ToList();
        foreach (var envelopeField in EnvelopeFieldNames)
        {
            Assert.DoesNotContain(envelopeField, names);
        }
    }

    private static GameStarted NewGameStarted() => new()
    {
        PlayerName = "Doc",
        StartingTownId = new TownId("town-1"),
        StartingTownName = "Dodge City",
        StartingHealth = 1000,
        StartingWallet = 25m,
        StartingInventoryItems = Array.Empty<InventoryItem>(),
        Difficulty = GameDifficulty.Standard,
        TravelRandomness = TravelRandomnessState.CreateDeterministic("test-salt"),
        Entropy = GameEntropy.Classic
    };

    private static StoreItemPurchased NewStoreItemPurchased() => new()
    {
        TownId = new TownId("town-1"),
        ItemKind = ItemKind.RevolverAmmo,
        DisplayName = "revolver ammo",
        Quantity = 3,
        UnitPrice = 2m,
        TotalPrice = 6m,
        WalletAfter = 19m
    };
}
