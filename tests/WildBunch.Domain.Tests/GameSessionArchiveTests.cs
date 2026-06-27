using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionArchiveTests
{
    [Fact]
    public void ArchivePlaythrough_Sets_Status_To_Archived()
    {
        var session = CreateSession();

        session.ArchivePlaythrough("start-over");

        Assert.Equal(GameStatus.Archived, session.Status);
    }

    [Fact]
    public void ArchivePlaythrough_Produces_PlaythroughArchived_Event_As_Uncommitted()
    {
        var session = CreateSession();

        session.ArchivePlaythrough("start-over");

        // StartNew emits GameStarted; archive appends PlaythroughArchived.
        var archived = session.UncommittedEvents.OfType<PlaythroughArchived>().Single();
        Assert.Equal("start-over", archived.ArchiveReason);
    }

    [Fact]
    public void ArchivePlaythrough_Increments_Version()
    {
        var session = CreateSession();
        var versionBefore = session.Version;

        session.ArchivePlaythrough("start-over");

        Assert.Equal(versionBefore + 1, session.Version);
    }

    [Fact]
    public void ArchivePlaythrough_Throws_On_Double_Archive()
    {
        var session = CreateSession();
        session.ArchivePlaythrough("start-over");

        var ex = Assert.Throws<InvalidOperationException>(() => session.ArchivePlaythrough("start-over"));
        Assert.Contains("already archived", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ArchivePlaythrough_Event_Carries_Correct_Derived_Metadata()
    {
        var session = CreateSession();
        var archivedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        session.ArchivePlaythrough("start-over", archivedAt);

        var archived = session.UncommittedEvents.OfType<PlaythroughArchived>().Single();
        Assert.Equal(archivedAt, archived.ArchivedAtUtc);
        Assert.Equal("start-over", archived.ArchiveReason);
        Assert.Equal("Ranger Vale", archived.PlayerName);
        Assert.Equal(new TownId("pinecross"), archived.LastTownId);
        Assert.Equal("Pinecross", archived.LastTownName);
        Assert.Equal(session.Clock.Day, archived.Day);
        Assert.Equal(session.Clock.TimeOfDay.ToString(), archived.Turn);
        Assert.Equal(GameStatus.Active, archived.StatusBeforeArchive);
    }

    [Fact]
    public void ArchivePlaythrough_Defaults_ArchivedAtUtc_To_Now_When_Not_Provided()
    {
        var session = CreateSession();
        var before = DateTime.UtcNow;

        session.ArchivePlaythrough("start-over");

        var after = DateTime.UtcNow;
        var archived = session.UncommittedEvents.OfType<PlaythroughArchived>().Single();
        Assert.InRange(archived.ArchivedAtUtc, before, after);
    }

    [Fact]
    public void ArchivePlaythrough_Replayed_From_Events_Restores_Archived_Status()
    {
        var session = CreateSession();
        session.ArchivePlaythrough("start-over", new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            session.World,
            CreateCaseFile(),
            session.CommittedEvents);

        Assert.Equal(GameStatus.Archived, rehydrated.Status);
    }

    private static GameSession CreateSession(
        Wallet? wallet = null,
        DomainInventory? inventory = null)
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var resolvedInventory = inventory ?? new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });
        return GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), wallet ?? Wallet.Starting(25m), resolvedInventory);
    }

    private static DomainWorld CreateWorld()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        return new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low)
            });
    }

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };
        return new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
    }
}
