using System.Text.Json;
using System.Text.Json.Serialization;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainInventoryItemKind = WildBunch.Domain.Inventory.ItemKind;
using DomainCanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    public string SerializePlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return JsonSerializer.Serialize(PlayerSnapshot.FromDomain(player), Options);
    }

    public Player DeserializePlayer(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<PlayerSnapshot>(json);
        return PlayerSnapshot.ToDomain(snapshot);
    }

    public string SerializeWorld(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return JsonSerializer.Serialize(WorldSnapshot.FromDomain(world), Options);
    }

    public World DeserializeWorld(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<WorldSnapshot>(json);
        return WorldSnapshot.ToDomain(snapshot);
    }

    public string SerializeCaseFile(CaseFile caseFile)
    {
        ArgumentNullException.ThrowIfNull(caseFile);
        return JsonSerializer.Serialize(CaseFileSnapshot.FromDomain(caseFile), Options);
    }

    public CaseFile DeserializeCaseFile(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<CaseFileSnapshot>(json);
        return CaseFileSnapshot.ToDomain(snapshot);
    }

    public string SerializeClock(GameClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return JsonSerializer.Serialize(GameClockSnapshot.FromDomain(clock), Options);
    }

    public GameClock DeserializeClock(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<GameClockSnapshot>(json);
        return GameClockSnapshot.ToDomain(snapshot);
    }

    public string SerializePursuitState(PursuitState pursuitState)
    {
        ArgumentNullException.ThrowIfNull(pursuitState);
        return JsonSerializer.Serialize(PursuitStateSnapshot.FromDomain(pursuitState), Options);
    }

    public PursuitState DeserializePursuitState(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<PursuitStateSnapshot>(json);
        return PursuitStateSnapshot.ToDomain(snapshot);
    }

    public string SerializeSaltSource(SaltSource saltSource)
    {
        ArgumentNullException.ThrowIfNull(saltSource);
        return JsonSerializer.Serialize(SaltSourceSnapshot.FromDomain(saltSource), Options);
    }

    public SaltSource DeserializeSaltSource(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<SaltSourceSnapshot>(json);
        return snapshot.ToDomain();
    }

    public string SerializeTownVisitState(TownVisitState townVisitState)
    {
        ArgumentNullException.ThrowIfNull(townVisitState);
        return JsonSerializer.Serialize(TownVisitStateSnapshot.FromDomain(townVisitState), Options);
    }

    public TownVisitState DeserializeTownVisitState(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<TownVisitStateSnapshot>(json);
        return snapshot.ToDomain();
    }

    public string SerializeCurrentActionContext(TownActionContext context, TownId? townId)
        => JsonSerializer.Serialize(new CurrentActionContextSnapshot(context, townId?.Value), Options);

    public (TownActionContext context, TownId? townId) DeserializeCurrentActionContext(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<CurrentActionContextSnapshot>(json);
        TownId? townId = snapshot.TownId is null ? null : new TownId(snapshot.TownId);
        return (snapshot.Context, townId);
    }

    private sealed record CurrentActionContextSnapshot(TownActionContext Context, string? TownId);

    public string? SerializePendingDevTravelOverride(DevTravelOverride? overrideValue)
        => overrideValue is null ? null : JsonSerializer.Serialize(overrideValue, Options);

    public DevTravelOverride? DeserializePendingDevTravelOverride(string? json)
        => json is null ? null : Deserialize<DevTravelOverride>(json);

    public string? SerializePendingDevSaloonOverride(DevSaloonOverride? overrideValue)
        => overrideValue is null ? null : JsonSerializer.Serialize(overrideValue, Options);

    public DevSaloonOverride? DeserializePendingDevSaloonOverride(string? json)
        => json is null ? null : Deserialize<DevSaloonOverride>(json);

    private sealed record PlayerSnapshot(
        string Name,
        string CurrentTownId,
        int Health,
        WalletSnapshot? Wallet,
        InventorySnapshot? Inventory)
    {
        public static PlayerSnapshot FromDomain(Player player)
            => new(
                player.Name,
                player.CurrentTownId.Value,
                player.Health,
                WalletSnapshot.FromDomain(player.Wallet),
                InventorySnapshot.FromDomain(player.Inventory));

        public static Player ToDomain(PlayerSnapshot snapshot)
            => new(
                snapshot.Name,
                new TownId(snapshot.CurrentTownId),
                snapshot.Health,
                WalletSnapshot.ToDomain(snapshot.Wallet),
                InventorySnapshot.ToDomain(snapshot.Inventory));
    }

    private sealed record WalletSnapshot(decimal Cash)
    {
        public static WalletSnapshot FromDomain(Wallet wallet)
            => new(wallet.Cash);

        public static Wallet ToDomain(WalletSnapshot? snapshot)
            => snapshot is null
                ? throw new InvalidOperationException("Unable to deserialize player wallet.")
                : new Wallet(snapshot.Cash);
    }

    private sealed record InventorySnapshot(IReadOnlyList<InventoryItemSnapshot> Items)
    {
        public static InventorySnapshot FromDomain(DomainInventory inventory)
            => new(inventory.Items.Select(InventoryItemSnapshot.FromDomain).ToArray());

        public static DomainInventory ToDomain(InventorySnapshot? snapshot)
            => snapshot is null
                ? DomainInventory.Empty()
                : new DomainInventory(snapshot.Items.Select(InventoryItemSnapshot.ToDomain));
    }

    private sealed record InventoryItemSnapshot(
        DomainInventoryItemKind Kind,
        int Quantity,
        DomainHorseTravelState? HorseState,
        DomainCanteenState? CanteenState)
    {
        public static InventoryItemSnapshot FromDomain(DomainInventoryItem item)
            => new(item.Kind, item.Quantity, item.HorseState, item.CanteenState);

        public static DomainInventoryItem ToDomain(InventoryItemSnapshot snapshot)
            => new(snapshot.Kind, snapshot.Quantity, snapshot.HorseState, snapshot.CanteenState);
    }

    private sealed record WorldSnapshot(IReadOnlyList<TownSnapshot> Towns, IReadOnlyList<TrailSnapshot> Trails)
    {
        public static WorldSnapshot FromDomain(World world)
            => new(
                world.Towns.Select(TownSnapshot.FromDomain).ToArray(),
                world.Trails.Select(TrailSnapshot.FromDomain).ToArray());

        public static World ToDomain(WorldSnapshot snapshot)
            => new(
                snapshot.Towns.Select(TownSnapshot.ToDomain),
                snapshot.Trails.Select(TrailSnapshot.ToDomain));
    }

    private sealed record TownSnapshot(string Id, string Name, TownServices Services)
    {
        public static TownSnapshot FromDomain(Town town)
            => new(town.Id.Value, town.Name, town.Services);

        public static Town ToDomain(TownSnapshot snapshot)
            => new(new TownId(snapshot.Id), snapshot.Name, snapshot.Services);
    }

    private sealed record TrailSnapshot(
        string Id,
        string FromTownId,
        string ToTownId,
        TrailRisk Risk,
        TrailTerrain Terrain,
        WaterFeature WaterFeature,
        decimal RideDayDistance)
    {
        public static TrailSnapshot FromDomain(Trail trail)
            => new(
                trail.Id.Value,
                trail.FromTownId.Value,
                trail.ToTownId.Value,
                trail.Risk,
                trail.Terrain,
                trail.WaterFeature,
                trail.RideDayDistance);

        public static Trail ToDomain(TrailSnapshot snapshot)
            => new(
                new TrailId(snapshot.Id),
                new TownId(snapshot.FromTownId),
                new TownId(snapshot.ToTownId),
                snapshot.Risk,
                snapshot.Terrain,
                snapshot.WaterFeature,
                snapshot.RideDayDistance);
    }

    private sealed record CaseFileSnapshot(
        string? AccusationId,
        string? OpeningLead,
        int KillerReleaseProgress,
        int KillerReleaseThreshold,
        IReadOnlyList<SuspectSnapshot> Suspects,
        IReadOnlyList<string>? DiscoveredSuspectIds,
        string TrueCulpritId,
        IReadOnlyList<ClueSnapshot> KnownClues,
        IReadOnlyList<ClueSnapshot>? PublicClues,
        IReadOnlyList<WarrantSnapshot>? KnownWarrants,
        IReadOnlyList<WarrantSnapshot>? PublicWarrants,
        IReadOnlyList<WantedSuspectConfrontationSnapshot>? WantedSuspectConfrontations,
        IReadOnlyList<SheriffTurnInSettlementSnapshot>? SheriffTurnInSettlements,
        IReadOnlyList<SuspectTurfAssignmentSnapshot>? SuspectTurfAssignments)
    {
        public static CaseFileSnapshot FromDomain(CaseFile caseFile)
            => new(
                caseFile.Accusation is null ? null : caseFile.Accusation.Value.Value,
                caseFile.OpeningLead.Description,
                caseFile.KillerReleaseProgress,
                caseFile.KillerReleaseThreshold,
                caseFile.Suspects.Select(SuspectSnapshot.FromDomain).ToArray(),
                caseFile.DiscoveredSuspectIds.Select(suspectId => suspectId.Value).ToArray(),
                caseFile.TrueCulpritId.Value,
                caseFile.KnownClues.Select(ClueSnapshot.FromDomain).ToArray(),
                caseFile.PublicClues.Select(ClueSnapshot.FromDomain).ToArray(),
                caseFile.KnownWarrants.Select(WarrantSnapshot.FromDomain).ToArray(),
                caseFile.PublicWarrants.Select(WarrantSnapshot.FromDomain).ToArray(),
                caseFile.WantedSuspectConfrontations.Select(WantedSuspectConfrontationSnapshot.FromDomain).ToArray(),
                caseFile.SheriffTurnInSettlements.Select(SheriffTurnInSettlementSnapshot.FromDomain).ToArray(),
                caseFile.SuspectTurfAssignments.Select(SuspectTurfAssignmentSnapshot.FromDomain).ToArray());

        public static CaseFile ToDomain(CaseFileSnapshot snapshot)
        {
            var caseFile = new CaseFile(
                snapshot.AccusationId is null ? null : new SuspectId(snapshot.AccusationId),
                snapshot.Suspects.Select(SuspectSnapshot.ToDomain),
                new SuspectId(snapshot.TrueCulpritId),
                CaseOpeningLead.Create(snapshot.OpeningLead ?? "Follow the public leads and look for a signature mark."),
                snapshot.KnownClues.Select(ClueSnapshot.ToDomain),
                (snapshot.DiscoveredSuspectIds ?? Array.Empty<string>()).Select(suspectId => new SuspectId(suspectId)),
                snapshot.PublicClues?.Select(ClueSnapshot.ToDomain),
                snapshot.KillerReleaseThreshold,
                snapshot.KillerReleaseProgress,
                (snapshot.KnownWarrants ?? Array.Empty<WarrantSnapshot>()).Select(WarrantSnapshot.ToDomain),
                (snapshot.PublicWarrants ?? Array.Empty<WarrantSnapshot>()).Select(WarrantSnapshot.ToDomain),
                suspectTurfAssignments: (snapshot.SuspectTurfAssignments ?? Array.Empty<SuspectTurfAssignmentSnapshot>()).Select(SuspectTurfAssignmentSnapshot.ToDomain),
                wantedSuspectConfrontations: (snapshot.WantedSuspectConfrontations ?? Array.Empty<WantedSuspectConfrontationSnapshot>()).Select(WantedSuspectConfrontationSnapshot.ToDomain),
                sheriffTurnInSettlements: (snapshot.SheriffTurnInSettlements ?? Array.Empty<SheriffTurnInSettlementSnapshot>()).Select(SheriffTurnInSettlementSnapshot.ToDomain));

            return caseFile;
        }
    }

    private sealed record SuspectTurfAssignmentSnapshot(string SuspectId, string TurfTownId)
    {
        public static SuspectTurfAssignmentSnapshot FromDomain(SuspectTurfAssignment assignment)
            => new(assignment.SuspectId.Value, assignment.TurfTownId.Value);

        public static SuspectTurfAssignment ToDomain(SuspectTurfAssignmentSnapshot snapshot)
            => new(new SuspectId(snapshot.SuspectId), new TownId(snapshot.TurfTownId));
    }

    private sealed record WantedSuspectConfrontationSnapshot(
        string SuspectId,
        string TargetName,
        WarrantDisposition Disposition,
        WantedSuspectConfrontationOutcome Outcome,
        bool IsAlive,
        bool IsSecured,
        int Day,
        int Turn)
    {
        public static WantedSuspectConfrontationSnapshot FromDomain(WantedSuspectConfrontationState state)
            => new(
                state.SuspectId.Value,
                state.TargetName,
                state.Disposition,
                state.Outcome,
                state.IsAlive,
                state.IsSecured,
                state.Day,
                state.Turn);

        public static WantedSuspectConfrontationState ToDomain(WantedSuspectConfrontationSnapshot snapshot)
            => new(
                new SuspectId(snapshot.SuspectId),
                snapshot.TargetName,
                snapshot.Disposition,
                snapshot.Outcome,
                snapshot.IsAlive,
                snapshot.IsSecured,
                snapshot.Day,
                snapshot.Turn);
    }

    private sealed record SheriffTurnInSettlementSnapshot(
        string SuspectId,
        string TargetName,
        WarrantDisposition Disposition,
        bool IsAlive,
        decimal BountyAmount,
        int Day,
        int Turn)
    {
        public static SheriffTurnInSettlementSnapshot FromDomain(SheriffTurnInSettlementState state)
            => new(
                state.SuspectId.Value,
                state.TargetName,
                state.Disposition,
                state.IsAlive,
                state.BountyAmount,
                state.Day,
                state.Turn);

        public static SheriffTurnInSettlementState ToDomain(SheriffTurnInSettlementSnapshot snapshot)
            => new(
                new SuspectId(snapshot.SuspectId),
                snapshot.TargetName,
                snapshot.Disposition,
                snapshot.IsAlive,
                snapshot.BountyAmount,
                snapshot.Day,
                snapshot.Turn);
    }

    private sealed record SuspectSnapshot(string Id, string Name, SuspectProfileSnapshot Profile, SuspectTraitsSnapshot Traits, SuspectStatus Status)
    {
        public static SuspectSnapshot FromDomain(Suspect suspect)
            => new(suspect.Id.Value, suspect.Name, SuspectProfileSnapshot.FromDomain(suspect.Profile), SuspectTraitsSnapshot.FromDomain(suspect.Traits), suspect.Status);

        public static Suspect ToDomain(SuspectSnapshot snapshot)
            => new(new SuspectId(snapshot.Id), snapshot.Name, SuspectProfileSnapshot.ToDomain(snapshot.Profile), SuspectTraitsSnapshot.ToDomain(snapshot.Traits), snapshot.Status);
    }

    private sealed record SuspectProfileSnapshot(IReadOnlyList<SuspectAliasSnapshot> Aliases, IReadOnlyList<SuspectIdentityFactSnapshot> IdentifyingFacts)
    {
        public static SuspectProfileSnapshot FromDomain(SuspectProfile profile)
            => new(
                profile.Aliases.Select(SuspectAliasSnapshot.FromDomain).ToArray(),
                profile.IdentifyingFacts.Select(SuspectIdentityFactSnapshot.FromDomain).ToArray());

        public static SuspectProfile ToDomain(SuspectProfileSnapshot snapshot)
            => new(
                (snapshot.Aliases ?? Array.Empty<SuspectAliasSnapshot>()).Select(SuspectAliasSnapshot.ToDomain),
                (snapshot.IdentifyingFacts ?? Array.Empty<SuspectIdentityFactSnapshot>()).Select(SuspectIdentityFactSnapshot.ToDomain));
    }

    private sealed record SuspectAliasSnapshot(string Name, AliasKind Kind)
    {
        public static SuspectAliasSnapshot FromDomain(SuspectAlias alias)
            => new(alias.Name, alias.Kind);

        public static SuspectAlias ToDomain(SuspectAliasSnapshot snapshot)
            => new(snapshot.Name, snapshot.Kind);
    }

    private sealed record SuspectIdentityFactSnapshot(string Description)
    {
        public static SuspectIdentityFactSnapshot FromDomain(SuspectIdentityFact fact)
            => new(fact.Description);

        public static SuspectIdentityFact ToDomain(SuspectIdentityFactSnapshot snapshot)
            => new(snapshot.Description);
    }

    private sealed record SuspectTraitsSnapshot(
        IReadOnlyList<string>? Tags,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsLocal,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsArmed,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsDesperate)
    {
        public static SuspectTraitsSnapshot FromDomain(SuspectTraits traits)
            => new(traits.Tags.Select(tag => tag.Value).ToArray(), null, null, null);

        public static SuspectTraits ToDomain(SuspectTraitsSnapshot snapshot)
        {
            if (snapshot.Tags is not null)
            {
                return new(snapshot.Tags.Select(tag => new SuspectTraitTag(tag)));
            }

            return SuspectTraits.FromLegacyFlags(
                snapshot.IsLocal == true,
                snapshot.IsArmed == true,
                snapshot.IsDesperate == true);
        }
    }

    private sealed record ClueSnapshot
    {
        public ClueSnapshot(
            string id,
            ClueKind kind,
            string description,
            IReadOnlyList<string>? linkedSuspectIds = null,
            InvestigationTargetKind targetKind = InvestigationTargetKind.Unknown,
            InvestigationSourceKind? sourceKind = null,
            string? source = null,
            string? context = null,
            ClueAnchorsSnapshot? anchors = null)
        {
            Id = id;
            Kind = kind;
            Description = description;
            LinkedSuspectIds = linkedSuspectIds;
            TargetKind = targetKind;
            SourceKind = sourceKind;
            Source = source;
            Context = context;
            Anchors = anchors;
        }

        public string Id { get; init; }
        public ClueKind Kind { get; init; }
        public string Description { get; init; }
        public IReadOnlyList<string>? LinkedSuspectIds { get; init; }
        public InvestigationTargetKind TargetKind { get; init; }
        public InvestigationSourceKind? SourceKind { get; init; }
        public string? Source { get; init; }
        public string? Context { get; init; }
        public ClueAnchorsSnapshot? Anchors { get; init; }

        public static ClueSnapshot FromDomain(Clue clue)
            => new(
                clue.Id.Value,
                clue.Kind,
                clue.Description,
                clue.LinkedSuspectIds.Select(suspectId => suspectId.Value).ToArray(),
                clue.TargetKind,
                clue.SourceKind,
                clue.Source,
                clue.Context,
                ClueAnchorsSnapshot.FromDomain(clue.Anchors));

        public static Clue ToDomain(ClueSnapshot snapshot)
            => new(
                new ClueId(snapshot.Id),
                snapshot.Kind,
                snapshot.Description,
                (snapshot.LinkedSuspectIds ?? Array.Empty<string>()).Select(suspectId => new SuspectId(suspectId)),
                snapshot.TargetKind,
                snapshot.SourceKind,
                snapshot.Source,
                snapshot.Context,
                snapshot.Anchors?.ToDomain()
                ?? ClueAnchors.FromLinkedSuspectIds((snapshot.LinkedSuspectIds ?? Array.Empty<string>()).Select(suspectId => new SuspectId(suspectId))));
    }

    private sealed record ClueAnchorsSnapshot(
        IReadOnlyList<ClueSubjectAnchorSnapshot>? Subjects,
        IReadOnlyList<ClueLocationAnchorSnapshot>? Locations,
        IReadOnlyList<ClueTimeAnchorSnapshot>? Times,
        IReadOnlyList<ClueDirectionAnchorSnapshot>? Directions)
    {
        public static ClueAnchorsSnapshot FromDomain(ClueAnchors anchors)
            => new(
                anchors.Subjects.Select(ClueSubjectAnchorSnapshot.FromDomain).ToArray(),
                anchors.Locations.Select(ClueLocationAnchorSnapshot.FromDomain).ToArray(),
                anchors.Times.Select(ClueTimeAnchorSnapshot.FromDomain).ToArray(),
                anchors.Directions.Select(ClueDirectionAnchorSnapshot.FromDomain).ToArray());

        public ClueAnchors ToDomain()
            => new(
                (Subjects ?? Array.Empty<ClueSubjectAnchorSnapshot>()).Select(ClueSubjectAnchorSnapshot.ToDomain),
                (Locations ?? Array.Empty<ClueLocationAnchorSnapshot>()).Select(ClueLocationAnchorSnapshot.ToDomain),
                (Times ?? Array.Empty<ClueTimeAnchorSnapshot>()).Select(ClueTimeAnchorSnapshot.ToDomain),
                (Directions ?? Array.Empty<ClueDirectionAnchorSnapshot>()).Select(ClueDirectionAnchorSnapshot.ToDomain));
    }

    private sealed record ClueSubjectAnchorSnapshot(
        string Label,
        string? SuspectId = null,
        string? Alias = null,
        string? Feature = null,
        string? Fact = null)
    {
        public static ClueSubjectAnchorSnapshot FromDomain(ClueSubjectAnchor anchor)
            => new(anchor.Label, anchor.SuspectId?.Value, anchor.Alias, anchor.Feature, anchor.Fact);

        public static ClueSubjectAnchor ToDomain(ClueSubjectAnchorSnapshot snapshot)
            => new(
                snapshot.Label,
                snapshot.SuspectId is null ? null : new SuspectId(snapshot.SuspectId),
                snapshot.Alias,
                snapshot.Feature,
                snapshot.Fact);
    }

    private sealed record ClueLocationAnchorSnapshot(
        string Label,
        string? TownId = null,
        string? Place = null,
        string? Route = null)
    {
        public static ClueLocationAnchorSnapshot FromDomain(ClueLocationAnchor anchor)
            => new(anchor.Label, anchor.TownId?.Value, anchor.Place, anchor.Route);

        public static ClueLocationAnchor ToDomain(ClueLocationAnchorSnapshot snapshot)
            => new(
                snapshot.Label,
                snapshot.TownId is null ? null : new TownId(snapshot.TownId),
                snapshot.Place,
                snapshot.Route);
    }

    private sealed record ClueTimeAnchorSnapshot(
        ClueRecency Recency,
        int? Day = null,
        int? Turn = null)
    {
        public static ClueTimeAnchorSnapshot FromDomain(ClueTimeAnchor anchor)
            => new(anchor.Recency, anchor.Day, anchor.Turn);

        public static ClueTimeAnchor ToDomain(ClueTimeAnchorSnapshot snapshot)
            => new(snapshot.Recency, snapshot.Day, snapshot.Turn);
    }

    private sealed record ClueDirectionAnchorSnapshot(
        string Label,
        string? Movement = null,
        string? DestinationTownId = null,
        string? Route = null)
    {
        public static ClueDirectionAnchorSnapshot FromDomain(ClueDirectionAnchor anchor)
            => new(anchor.Label, anchor.Movement, anchor.DestinationTownId?.Value, anchor.Route);

        public static ClueDirectionAnchor ToDomain(ClueDirectionAnchorSnapshot snapshot)
            => new(
                snapshot.Label,
                snapshot.Movement,
                snapshot.DestinationTownId is null ? null : new TownId(snapshot.DestinationTownId),
                snapshot.Route);
    }

    private sealed record WarrantSnapshot(
        string Id,
        string TargetName,
        WarrantTermsSnapshot Terms,
        string Summary)
    {
        public static WarrantSnapshot FromDomain(Warrant warrant)
            => new(
                warrant.Id.Value,
                warrant.TargetName,
                WarrantTermsSnapshot.FromDomain(warrant.Terms),
                warrant.Summary);

        public static Warrant ToDomain(WarrantSnapshot snapshot)
            => new(
                new WarrantId(snapshot.Id),
                snapshot.TargetName,
                WarrantTermsSnapshot.ToDomain(snapshot.Terms),
                snapshot.Summary);
    }

    private sealed record WarrantTermsSnapshot(
        WarrantDisposition Disposition,
        decimal BountyAmount,
        IReadOnlyList<string>? KnownAliases,
        IReadOnlyList<string>? KnownFeatures,
        string IssuingSource,
        InvestigationTargetKind TargetKind,
        IReadOnlyList<OutlawGangId>? GangAffiliations,
        OutlawGangId? AdvancesGangPressureFor,
        InvestigationSourceKind? SourceKind,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsGangRelevant,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? AdvancesGangPressure)
    {
        public static WarrantTermsSnapshot FromDomain(WarrantTerms terms)
            => new(
                terms.Disposition,
                terms.BountyAmount,
                terms.KnownAliases.ToArray(),
                terms.KnownFeatures.ToArray(),
                terms.IssuingSource,
                terms.TargetKind,
                terms.GangAffiliations.ToArray(),
                terms.AdvancesGangPressureFor,
                terms.SourceKind,
                null,
                null);

        public static WarrantTerms ToDomain(WarrantTermsSnapshot snapshot)
            => new(
                snapshot.Disposition,
                snapshot.BountyAmount,
                snapshot.KnownAliases ?? Array.Empty<string>(),
                snapshot.KnownFeatures ?? Array.Empty<string>(),
                snapshot.IssuingSource,
                snapshot.TargetKind,
                snapshot.GangAffiliations ?? ((snapshot.IsGangRelevant == true || snapshot.AdvancesGangPressure == true) ? [OutlawGangIds.WildBunch] : []),
                snapshot.AdvancesGangPressureFor ?? (snapshot.AdvancesGangPressure == true ? OutlawGangIds.WildBunch : null),
                snapshot.SourceKind);
    }

    private sealed record PursuitStateSnapshot(int Heat)
    {
        public static PursuitStateSnapshot FromDomain(PursuitState pursuitState)
            => new(pursuitState.Heat);

        public static PursuitState ToDomain(PursuitStateSnapshot snapshot)
        {
            var pursuitState = new PursuitState();
            GameSessionRehydrator.SetBackingField(pursuitState, "<Heat>k__BackingField", snapshot.Heat);
            return pursuitState;
        }
    }

    private sealed record GameClockSnapshot(int Day, int Turn)
    {
        public static GameClockSnapshot FromDomain(GameClock clock)
            => new(clock.Day, clock.Turn);

        public static GameClock ToDomain(GameClockSnapshot snapshot)
        {
            var clock = new GameClock();
            GameSessionRehydrator.SetBackingField(clock, "<Day>k__BackingField", snapshot.Day);
            GameSessionRehydrator.SetBackingField(clock, "<Turn>k__BackingField", snapshot.Turn);
            return clock;
        }
    }

    private sealed record SaltSourceSnapshot(SaltSourceMode Mode, string Salt)
    {
        public static SaltSourceSnapshot FromDomain(SaltSource saltSource)
            => new(saltSource.Mode, saltSource.Salt);

        public SaltSource ToDomain()
            => new(Mode, Salt);
    }

    private sealed record TownVisitStateSnapshot(
        string TownId,
        IReadOnlyList<TownVisitTownStateSnapshot>? TownStates,
        IReadOnlyList<InvestigationSourceKind>? SpentInvestigationSources,
        bool WantedPostersSpent)
    {
        public static TownVisitStateSnapshot FromDomain(TownVisitState townVisitState)
            => new(
                townVisitState.CurrentTownId.Value,
                townVisitState.TownStates
                    .OrderBy(townState => townState.TownId.Value, StringComparer.Ordinal)
                    .Select(TownVisitTownStateSnapshot.FromDomain)
                    .ToArray(),
                townVisitState.SpentInvestigationSources.ToArray(),
                townVisitState.WantedPostersSpent);

        public TownVisitState ToDomain()
        {
            if (TownStates is { Count: > 0 })
            {
                return TownVisitState.FromTownStates(
                    new TownId(TownId),
                    TownStates.Select(townState => townState.ToDomain()));
            }

            return TownVisitState.FromLegacy(
                new TownId(TownId),
                (SpentInvestigationSources ?? Array.Empty<InvestigationSourceKind>()).Select(sourceKind => sourceKind),
                WantedPostersSpent);
        }
    }

    private sealed record TownVisitTownStateSnapshot(
        string TownId,
        int VisitNumber,
        IReadOnlyList<TownSourceVisitStateSnapshot>? SourceStates,
        int WantedPostersLastCheckedVisitNumber,
        string? ActiveSaloonPersonOfInterestId,
        string? ActiveSaloonPersonOfInterestDescriptor,
        SaloonPersonOfInterestKind? ActiveSaloonPersonOfInterestKind)
    {
        public static TownVisitTownStateSnapshot FromDomain(TownVisitTownState townState)
            => new(
                townState.TownId.Value,
                townState.VisitNumber,
                townState.SourceStates
                    .OrderBy(sourceState => sourceState.SourceKind)
                    .Select(TownSourceVisitStateSnapshot.FromDomain)
                    .ToArray(),
                townState.WantedPostersLastCheckedVisitNumber,
                townState.ActiveSaloonPersonOfInterestId?.Value,
                townState.ActiveSaloonPersonOfInterestDescriptor,
                townState.ActiveSaloonPersonOfInterestKind);

        public TownVisitTownState ToDomain()
            => new(
                new TownId(TownId),
                VisitNumber,
                SourceStates?.Select(snapshot => snapshot.ToDomain()),
                wantedPostersSpent: WantedPostersLastCheckedVisitNumber == VisitNumber,
                activeSaloonPersonOfInterestId: ActiveSaloonPersonOfInterestId is null ? null : new SuspectId(ActiveSaloonPersonOfInterestId),
                activeSaloonPersonOfInterestDescriptor: ActiveSaloonPersonOfInterestDescriptor,
                activeSaloonPersonOfInterestKind: ActiveSaloonPersonOfInterestKind);
    }

    private sealed record TownSourceVisitStateSnapshot(
        string TownId,
        InvestigationSourceKind SourceKind,
        TownSourceRefreshPolicy RefreshPolicy,
        int LastRefreshedVisitNumber,
        int LastCheckedVisitNumber)
    {
        public static TownSourceVisitStateSnapshot FromDomain(TownSourceVisitState sourceState)
            => new(
                sourceState.TownId.Value,
                sourceState.SourceKind,
                sourceState.RefreshPolicy,
                sourceState.LastRefreshedVisitNumber,
                sourceState.LastCheckedVisitNumber);

        public TownSourceVisitState ToDomain()
            => new(
                new TownId(TownId),
                SourceKind,
                RefreshPolicy,
                LastRefreshedVisitNumber,
                LastCheckedVisitNumber);
    }
}
