# Store, prosperity and inventory boundaries

**Status:** Working feature partition following the user's correction. Product rules below are supplied requirements, not certification that current code implements them. This is design discovery, not an implementation plan.

## Product boundaries

PG-004 is an umbrella requiring decomposition. Buying goods, town prosperity, horse lifecycle, canteen lifecycle, equipment ownership and consumable quantities are connected contracts rather than one store feature. Preserve the umbrella ID for traceability; the [feature matrix](../../../docs/features.md) records the current grouping and the separate store-consolidation and future-vendor work boundaries.

| Boundary | Supplied product contract | Dependencies |
| --- | --- | --- |
| Single town store and purchasing | One store type today. Remove live distinctions for gunsmiths, stables and other vendor types; record their future return separately. | Consumes town prosperity for offerings; changes cash and inventory through events. |
| Town prosperity | A first-class town fact that services can use to vary stock and prices. | Supplies the store; assess other service consumers separately. |
| Horse lifecycle | At most one horse. Death removes it from inventory; the saddle is retained. A player with no horse can buy one, while a second horse must not charge them for a useless duplicate. | Travel establishes death and mounted capability; purchasing acquires a horse. |
| Canteen lifecycle | At most one canteen. Town arrival automatically refills an owned canteen; it does not give a canteen to a player without one. | Travel consumes water; arrival refills; purchasing establishes ownership. |
| Nonstackable equipment | At most one of each distinct equipment type: a pistol and rifle can coexist, duplicate rifles or knives cannot. | Purchasing and inventory enforce ownership; encounters consume equipment capabilities. |
| Consumable quantities | Food, horse food, medicine and ammunition are stackable; the player can buy quantities they can afford without an arbitrary single-item ownership cap. | Purchasing changes quantities; corresponding gameplay consumes them. |

The original game's Colt .44 and Colt .45 are distinct item types, each individually nonstackable, with separately stackable compatible ammunition and weapon-specific encounter damage. Record this as a feature-shaped original-game reference to assess for future scope, not proof it currently ships or an instruction to add weapon variants during cleanup. Likewise, snakebite medicine is supplied as a consumable example; current ItemKind contains food, horse feed, canteen, horse, saddle, knife, revolver/revolver ammunition and rifle/rifle ammunition, but no medicine. Track its delivery status and consumer before claiming it ships.

## PG-004-R: remove separate-vendor distinctions now

**Story:** As a player in a town, I buy the goods offered by that town's one store, at prices driven by the town's prosperity. I am not asked to choose a stable or gunsmith.

The previous implementation composed separate general-store, stable and gunsmith offer tables. The completed consolidation removes vendor identity from the Domain catalog, Application command/read DTOs/mapping, API request, Web types/adapter/purchase UI and affected fixtures/tests. The browser submits item kind and quantity, and the server selects only an item present in the current town's catalog. The purchase event already records item, town, quantity and price without vendor identity, so the consolidation needs no event or persistence schema change.

Consolidate existing goods into the single store rather than accidentally deleting horses, saddles or weapons with their old vendor tables. Preserve prosperity-based availability and price variation deliberately. One store has one price per offered item at each prosperity tier. Do not preserve vendor-dependent prices or duplicate offers after removing vendors.

StoreItemPurchased records item, town, quantity and price without vendor identity, so the inspected purchase event does not require a vendor-field migration. Tests protect the prosperity matrix, lawful item-only purchases, exact cash/quantity effects and failure without a purchase charge; vendor-distinction assertions were retired.

## Dated implementation outcome: 2026-10-08

The one-store catalog preserves the existing offered-item union for Boomtown and Prosperous towns (Food, Horse feed, Canteen, Knife, Horse, Saddle, Revolver, Revolver ammo and Rifle ammo), Poor towns (Food, Horse feed, Canteen, Horse and Saddle), and Destitute towns (Food and Horse feed). Horse feed now uses the general-store price already present at each tier: $1.00 in Boomtown and Prosperous, $1.25 in Poor, and $1.50 in Destitute. The catalog and purchase request/response expose no vendor, availability or source-provenance fields; listing an item means it can be purchased. `Rifle` remains a valid inventory kind with no current offer or starting grant, so this slice does not claim rifle acquisition is complete. The store consolidation does not change `StoreItemPurchased` or persistence schemas. The initial catalog test failed on the duplicate Boomtown Horse feed offer before the catalog was consolidated, and the Domain, Application, HTTP/PostgreSQL and browser behavior checks now cover the selected contract.

## PG-004-A: restore differentiated town services later

**Story:** As a player, I can visit distinct town services whose availability and offerings have gameplay meaning, such as a stable for horses/tack and a gunsmith for firearms/ammunition.

Reintroduction requires explicit service identity and town availability, reachable navigation, service-specific catalogs/prices, server validation of purchase location/service, and integration with inventory, horse and weapon rules. Economics based on which vendor the player buys from belongs to this future feature, as explicitly assigned by the user. Decide how prosperity influences each service and whether visiting/purchasing changes time. Add player-safe read contracts and meaningful behavioral coverage for unavailable services, invalid remote/service purchases and legal acquisition. A vendor enum on an aggregated store table alone does not deliver this feature. This job is deferred; it does not authorize building those services in 0.1.0.

## Lifecycle verification and correction

The previous recommendation about replacing an inventory-carried dead horse or buying a replacement empty canteen was unsupported and conflicts with the user's contract. Verify horse death removes only the horse through live command and replay; verify retained saddle and ability to buy a horse afterward. Verify owned-canteen refill on town arrival and absence of canteen creation when unowned. RefillCanteenAfterArrival already checks ownership and refills capacity in current source; do not invent a purchase-based refill action.

For purchases, preserve one-per-type equipment and quantity-based consumables, including unaffordable, nonpositive and duplicate-equipment failures. Verify actual gameplay consumption/damage rather than declaring all enum values delivered. Separate pending/failure UI and CQRS read-repository corrections from product scope. These obligations span PG-004 purchasing, PG-006 confrontation and PG-007 travel.
