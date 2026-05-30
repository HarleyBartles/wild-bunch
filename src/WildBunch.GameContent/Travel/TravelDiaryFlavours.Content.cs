using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.Travel;

public static partial class TravelDiaryFlavourCatalog
{
    private static TravelDiaryFlavourEntry[] BuildEntries()
        => [
            ..BuildDayOpeningEntries(),
            ..BuildQuietTextureEntries(),
            ..BuildLuckyEventEntries(),
            ..BuildUnluckyEventEntries(),
            ..BuildFoeEncounterIntroEntries(),
            ..BuildResourceScarcityEntries(),
            ..BuildWaterScarcityEntries(),
            ..BuildWaterReliefEntries(),
            ..BuildHorsePressureEntries(),
            ..BuildChoiceOutcomeEntries(),
            ..BuildArrivalCompletionEntries()
        ];

    private static TravelDiaryFlavourEntry[] BuildDayOpeningEntries()
        => [
            Entry("diary.day-opening.open-range-1", TravelDiaryFlavourCategory.DayOpening, "I started the day with the open range laid out flat and patient before me.", tags: ["opening", "open-range"], terrain: TrailTerrain.OpenRange),
            Entry("diary.day-opening.open-range-2", TravelDiaryFlavourCategory.DayOpening, "I started the day with grass, dust, and a long view that did not care about my plans.", tags: ["opening", "open-range"], terrain: TrailTerrain.OpenRange),
            Entry("diary.day-opening.hills-1", TravelDiaryFlavourCategory.DayOpening, "I started the day with the hills shouldering up ahead of me.", tags: ["opening", "hills"], terrain: TrailTerrain.Hills),
            Entry("diary.day-opening.hills-2", TravelDiaryFlavourCategory.DayOpening, "I started the day knowing the hills would take their toll one slope at a time.", tags: ["opening", "hills"], terrain: TrailTerrain.Hills),
            Entry("diary.day-opening.badlands-1", TravelDiaryFlavourCategory.DayOpening, "I started the day with hard stone underfoot and badlands ahead.", tags: ["opening", "badlands"], terrain: TrailTerrain.Badlands),
            Entry("diary.day-opening.badlands-2", TravelDiaryFlavourCategory.DayOpening, "I started the day with the badlands looking mean about it and me trying not to argue.", tags: ["opening", "badlands"], terrain: TrailTerrain.Badlands),
            Entry("diary.day-opening.mountains-1", TravelDiaryFlavourCategory.DayOpening, "I started the day with the mountains rising like a warning.", tags: ["opening", "mountains"], terrain: TrailTerrain.Mountains),
            Entry("diary.day-opening.general-1", TravelDiaryFlavourCategory.DayOpening, "I started the day with my hat low and the trail already asking questions.", tags: ["opening", "general"])
        ];

    private static TravelDiaryFlavourEntry[] BuildQuietTextureEntries()
        => [
            Entry("diary.terrain.open-range-1", TravelDiaryFlavourCategory.QuietTexture, "The open range kept me honest, and the wind did most of the talking.", tags: ["quiet", "open-range"], terrain: TrailTerrain.OpenRange),
            Entry("diary.terrain.open-range-2", TravelDiaryFlavourCategory.QuietTexture, "I rode through miles of grass and dust, with only hawk shadows for company.", tags: ["quiet", "open-range", "hawks"], terrain: TrailTerrain.OpenRange),
            Entry("diary.terrain.hills-1", TravelDiaryFlavourCategory.QuietTexture, "The hills broke the trail into small hard victories.", tags: ["quiet", "hills"], terrain: TrailTerrain.Hills),
            Entry("diary.terrain.hills-2", TravelDiaryFlavourCategory.QuietTexture, "I passed through rolling country where every ridge hid the next one.", tags: ["quiet", "hills"], terrain: TrailTerrain.Hills),
            Entry("diary.terrain.badlands-1", TravelDiaryFlavourCategory.QuietTexture, "The badlands stripped the world down to stone, glare, and dry silence.", tags: ["quiet", "badlands"], terrain: TrailTerrain.Badlands),
            Entry("diary.terrain.badlands-2", TravelDiaryFlavourCategory.QuietTexture, "Broken ground and sharp cut banks kept me watching my step.", tags: ["quiet", "badlands", "tracks"], terrain: TrailTerrain.Badlands),
            Entry("diary.terrain.mountains-1", TravelDiaryFlavourCategory.QuietTexture, "The mountain trail stayed narrow and stubborn, like it resented every boot print.", tags: ["quiet", "mountains"], terrain: TrailTerrain.Mountains),
            Entry("diary.terrain.mountains-2", TravelDiaryFlavourCategory.QuietTexture, "The ridgelines kept the wind busy and the views too wide for comfort.", tags: ["quiet", "mountains", "wind"], terrain: TrailTerrain.Mountains),
            Entry("diary.terrain.campfire-1", TravelDiaryFlavourCategory.QuietTexture, "I passed a cold campfire and old tracks that still pointed the right way.", tags: ["quiet", "tracks", "camp"]),
            Entry("diary.terrain.peddler-1", TravelDiaryFlavourCategory.QuietTexture, "I passed a peddler's wagon far off the trail and left it to its own business.", tags: ["quiet", "peddler", "wagon"]),
            Entry("diary.terrain.ranch-1", TravelDiaryFlavourCategory.QuietTexture, "I rode past a broken fence and a ranch hand mending what the wind had already tested.", tags: ["quiet", "ranch", "fence"]),
            Entry("diary.terrain.smoke-1", TravelDiaryFlavourCategory.QuietTexture, "I saw distant smoke hanging over a camp I never reached, and I kept to my own road.", tags: ["quiet", "smoke", "camp"])
        ];

    private static TravelDiaryFlavourEntry[] BuildLuckyEventEntries()
        => [
            Entry("diary.lucky.coin-cache-1", TravelDiaryFlavourCategory.LuckyEvent, "I found a little luck in the dust and tucked it away before the trail noticed.", tags: ["lucky", "coin"]),
            Entry("diary.lucky.food-cache-1", TravelDiaryFlavourCategory.LuckyEvent, "I stumbled onto a cache of trail grub and let myself grin about it.", tags: ["lucky", "food"]),
            Entry("diary.lucky.water-seep-1", TravelDiaryFlavourCategory.LuckyEvent, "I caught a hidden seep and felt the day loosen its grip on me.", tags: ["lucky", "water"]),
            Entry("diary.lucky-waypoint-1", TravelDiaryFlavourCategory.LuckyEvent, "I found the old track right where I needed it and saved a hard detour.", tags: ["lucky", "trail"]),
            Entry("diary.lucky-shade-1", TravelDiaryFlavourCategory.LuckyEvent, "I found a strip of shade by the creek and counted it as a kindness.", tags: ["lucky", "water", "shade"]),
            Entry("diary.lucky-scrap-1", TravelDiaryFlavourCategory.LuckyEvent, "A trader left behind a useful scrap, and I was quick enough to keep it.", tags: ["lucky", "trader"]),
            Entry("diary.lucky-spring-1", TravelDiaryFlavourCategory.LuckyEvent, "I came on a spring still holding water, and the day stopped grinding so hard.", tags: ["lucky", "water", "spring"]),
            Entry("diary.lucky-marker-1", TravelDiaryFlavourCategory.LuckyEvent, "A weathered marker pointed me where I meant to go, which felt like a favor.", tags: ["lucky", "trail"]),
            Entry("diary.lucky-crossing-1", TravelDiaryFlavourCategory.LuckyEvent, "I found a clean crossing and saved myself a mess of delay.", tags: ["lucky", "trail", "crossing"]),
            Entry("diary.lucky-coffee-1", TravelDiaryFlavourCategory.LuckyEvent, "I met a traveling camp with enough coffee to share, and I did not waste the moment.", tags: ["lucky", "camp", "traveller"]),
            Entry("diary.lucky-wire-1", TravelDiaryFlavourCategory.LuckyEvent, "I spotted a length of usable wire by the road and pocketed it before the dust could object.", tags: ["lucky", "camp"]),
            Entry("diary.lucky-quiet-mile-1", TravelDiaryFlavourCategory.LuckyEvent, "I got one quiet mile where nothing asked for payment, and I took it.", tags: ["lucky", "mile"])
        ];

    private static TravelDiaryFlavourEntry[] BuildUnluckyEventEntries()
        => [
            Entry("diary.unlucky.washout-1", TravelDiaryFlavourCategory.UnluckyEvent, "A washout made me work for every inch, and I did not get to complain about it.", tags: ["unlucky", "weather"]),
            Entry("diary.unlucky.food-loss-1", TravelDiaryFlavourCategory.UnluckyEvent, "A rough patch cost me supplies, and the trail kept its poker face.", tags: ["unlucky", "food"]),
            Entry("diary.unlucky.spooked-horse-1", TravelDiaryFlavourCategory.UnluckyEvent, "My horse jumped at the wrong noise, and I spent the rest of the day paying for it.", tags: ["unlucky", "horse"], requiresHorsePresent: true),
            Entry("diary.unlucky.dust-storm-1", TravelDiaryFlavourCategory.UnluckyEvent, "The dust storm rolled through like it had a grudge against me.", tags: ["unlucky", "dust"]),
            Entry("diary.unlucky-wheel-1", TravelDiaryFlavourCategory.UnluckyEvent, "A wheel rutted hard and stole time I did not mean to spend.", tags: ["unlucky", "wagon"]),
            Entry("diary.unlucky-gully-1", TravelDiaryFlavourCategory.UnluckyEvent, "A gullied stretch of trail shook loose my patience and some daylight.", tags: ["unlucky", "trail"]),
            Entry("diary.unlucky-vultures-1", TravelDiaryFlavourCategory.UnluckyEvent, "Vultures kept circling ground I would rather not investigate.", tags: ["unlucky", "vultures"]),
            Entry("diary.unlucky-fence-1", TravelDiaryFlavourCategory.UnluckyEvent, "A broken fence line and scattered brush slowed me down more than they ought to.", tags: ["unlucky", "fence"]),
            Entry("diary.unlucky-rain-1", TravelDiaryFlavourCategory.UnluckyEvent, "Cold rain turned the trail slick, and every step felt longer than it should.", tags: ["unlucky", "weather"]),
            Entry("diary.unlucky-fork-1", TravelDiaryFlavourCategory.UnluckyEvent, "I took a wrong fork and burned daylight sorting out my mistake.", tags: ["unlucky", "trail"]),
            Entry("diary.unlucky-camp-1", TravelDiaryFlavourCategory.UnluckyEvent, "I came on an abandoned camp and found nothing there but the feeling of being late.", tags: ["unlucky", "camp"]),
            Entry("diary.unlucky-grit-1", TravelDiaryFlavourCategory.UnluckyEvent, "The wind threw grit in my face until the day felt spiteful.", tags: ["unlucky", "wind"])
        ];

    private static TravelDiaryFlavourEntry[] BuildFoeEncounterIntroEntries()
        => [
            Entry("diary.foe.intro-road-agent-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A road agent squared up ahead of me, and I kept my hand close.", tags: ["foe", "road-agent"]),
            Entry("diary.foe.intro-bandit-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A bandit showed himself early, which meant the day just got meaner.", tags: ["foe", "bandit"]),
            Entry("diary.foe.intro-deserter-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A deserter cut a hard line across the trail and left me no easy read.", tags: ["foe", "deserter"]),
            Entry("diary.foe.intro-deputy-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A crooked deputy rode in with a look I did not trust.", tags: ["foe", "crooked-deputy"]),
            Entry("diary.foe.intro-bounty-hunter-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A worn-out bounty hunter blocked the way, tired but still stubborn about it.", tags: ["foe", "bounty-hunter"]),
            Entry("diary.foe.intro-rider-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A hard-eyed rider cut across my path, and I kept my hand close.", tags: ["foe", "hard-eyed-rider"]),
            Entry("diary.foe.intro-claim-jumper-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A claim jumper stood where he ought not be, which told me plenty.", tags: ["foe", "claim-jumper"]),
            Entry("diary.foe.intro-cattle-thief-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A cattle thief drifted out of the brush and thought about my road too long.", tags: ["foe", "cattle-thief"]),
            Entry("diary.foe.intro-drifter-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A desperate drifter stopped pretending not to notice me.", tags: ["foe", "drifter"]),
            Entry("diary.foe.intro-hired-gun-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A hired gun rode in with a cold look and no wasted motion.", tags: ["foe", "hired-gun"]),
            Entry("diary.foe.intro-scout-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A suspicious scout blocked the trail, and I did not like the look of that pause.", tags: ["foe", "scout"]),
            Entry("diary.foe.intro-trail-blocker-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A trail blocker had picked a bad place to make a stand.", tags: ["foe", "trail-blocker"]),
            Entry("diary.foe.intro-rider-2", TravelDiaryFlavourCategory.FoeEncounterIntro, "A low-slung rider waited where the trail narrowed, making the whole road feel smaller.", tags: ["foe", "rider"]),
            Entry("diary.foe.intro-stranger-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A sour-looking stranger turned his horse across my path and waited me out.", tags: ["foe", "stranger"]),
            Entry("diary.foe.intro-pair-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A pair of riders stopped talking when they saw me, which was answer enough.", tags: ["foe", "pair"]),
            Entry("diary.foe.intro-brush-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A man with a mean look and no hurry rode out of the brush.", tags: ["foe", "brush"])
        ];

    private static TravelDiaryFlavourEntry[] BuildResourceScarcityEntries()
        => [
            Entry("diary.resources.low-food-1", TravelDiaryFlavourCategory.ResourceScarcity, "My food was getting thin, and I counted every bite like it mattered.", tags: ["resource", "food"]),
            Entry("diary.resources.low-food-2", TravelDiaryFlavourCategory.ResourceScarcity, "I was down to the kind of provisions that made a man think ahead.", tags: ["resource", "food"]),
            Entry("diary.resources.low-food-3", TravelDiaryFlavourCategory.ResourceScarcity, "I measured supper before noon because I had to.", tags: ["resource", "food"]),
            Entry("diary.resources.low-food-4", TravelDiaryFlavourCategory.ResourceScarcity, "The trail kept me honest about how little I had left to eat.", tags: ["resource", "food"]),
            Entry("diary.resources.low-feed-1", TravelDiaryFlavourCategory.ResourceScarcity, "My horse feed was running low, so I kept a close eye on the next stop.", tags: ["resource", "horse-feed"], requiresHorsePresent: true),
            Entry("diary.resources.low-feed-2", TravelDiaryFlavourCategory.ResourceScarcity, "I did not have much horse feed left, and the horse knew it before I said a word.", tags: ["resource", "horse-feed"], requiresHorsePresent: true),
            Entry("diary.resources.low-supplies-1", TravelDiaryFlavourCategory.ResourceScarcity, "My saddlebag was lighter than it ought to be, and I felt every missing ounce.", tags: ["resource", "supplies"]),
            Entry("diary.resources.low-supplies-2", TravelDiaryFlavourCategory.ResourceScarcity, "I kept thinking about the next town because my pack was not keeping up.", tags: ["resource", "supplies"]),
            Entry("diary.resources.low-general-1", TravelDiaryFlavourCategory.ResourceScarcity, "The trail had me watching food, feed, and time like a gambler watched a table.", tags: ["resource", "general"]),
            Entry("diary.resources.low-general-2", TravelDiaryFlavourCategory.ResourceScarcity, "I was guarding my last matches, my last mouthfuls, and my temper.", tags: ["resource", "general"])
        ];

    private static TravelDiaryFlavourEntry[] BuildWaterScarcityEntries()
        => [
            Entry("diary.water.dry-canteen-1", TravelDiaryFlavourCategory.WaterScarcity, "My canteen had grown light, and the trail was not handing out mercy.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-2", TravelDiaryFlavourCategory.WaterScarcity, "Every dry mile reminded me to keep the canteen close and my temper closer.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-3", TravelDiaryFlavourCategory.WaterScarcity, "I was watching water the way a prospector watched a last gold speck.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-4", TravelDiaryFlavourCategory.WaterScarcity, "The dry trail kept asking questions my canteen could not answer for long.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-5", TravelDiaryFlavourCategory.WaterScarcity, "I rode on hope and a shallow canteen, which was no way to stay cheerful.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-6", TravelDiaryFlavourCategory.WaterScarcity, "The sun kept asking for more water than I had to give.", tags: ["water", "dry"], requiresRouteWaterSecure: false)
        ];

    private static TravelDiaryFlavourEntry[] BuildWaterReliefEntries()
        => [
            Entry("diary.water.relief-1", TravelDiaryFlavourCategory.WaterRelief, "I got to breathe easier when the water held and the canteen stopped feeling so loud.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-2", TravelDiaryFlavourCategory.WaterRelief, "A good stretch of water made the trail feel a little less personal.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-3", TravelDiaryFlavourCategory.WaterRelief, "I found enough water to keep my head straight and my pace honest.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-4", TravelDiaryFlavourCategory.WaterRelief, "The route water held, and that alone bought me some peace.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-5", TravelDiaryFlavourCategory.WaterRelief, "A spring gave me room to breathe, and I did not waste the chance.", tags: ["water", "relief", "spring"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-6", TravelDiaryFlavourCategory.WaterRelief, "The canteen stopped rattling so hard once I knew the next water was coming.", tags: ["water", "relief"], requiresRouteWaterSecure: true)
        ];

    private static TravelDiaryFlavourEntry[] BuildHorsePressureEntries()
        => [
            Entry("diary.horse.pressure-1", TravelDiaryFlavourCategory.HorsePressure, "My horse had felt the miles, so I kept one eye on every step.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-2", TravelDiaryFlavourCategory.HorsePressure, "The horse had worked hard enough to earn my attention, and I gave it freely.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-3", TravelDiaryFlavourCategory.HorsePressure, "I could feel the horse starting to wear down, and I did not like what that meant for tomorrow.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-4", TravelDiaryFlavourCategory.HorsePressure, "The horse took the strain, and I made a note to treat it better than I was treated.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-5", TravelDiaryFlavourCategory.HorsePressure, "My horse was blowing hard enough that I kept easing my hand.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-6", TravelDiaryFlavourCategory.HorsePressure, "I could feel the horse starting to lag, and I did not like the look of tomorrow.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-7", TravelDiaryFlavourCategory.HorsePressure, "The horse took the climb badly, and I took the hint.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-8", TravelDiaryFlavourCategory.HorsePressure, "I kept checking the horse's step and pretending that counted as comfort.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-9", TravelDiaryFlavourCategory.HorsePressure, "The horse was tired enough that every mile felt borrowed.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-10", TravelDiaryFlavourCategory.HorsePressure, "I slowed the pace some, because pride did not feed a horse.", tags: ["horse", "pressure"], requiresHorsePresent: true)
        ];

    private static TravelDiaryFlavourEntry[] BuildChoiceOutcomeEntries()
        => [
            Entry("diary.choice.run-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I ran for it and let the dust think it won.", tags: ["choice", "run"]),
            Entry("diary.choice.run-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I chose speed over pride and kept moving.", tags: ["choice", "run"]),
            Entry("diary.choice.run-3", TravelDiaryFlavourCategory.ChoiceOutcome, "I put distance between me and trouble before trouble could get clever.", tags: ["choice", "run"]),
            Entry("diary.choice.run-4", TravelDiaryFlavourCategory.ChoiceOutcome, "I took the wiser road and left the hard questions behind.", tags: ["choice", "run"]),
            Entry("diary.choice.run-5", TravelDiaryFlavourCategory.ChoiceOutcome, "I gave the trail my back and trusted my legs.", tags: ["choice", "run"]),
            Entry("diary.choice.run-6", TravelDiaryFlavourCategory.ChoiceOutcome, "I slipped away while there was still time to do it clean.", tags: ["choice", "run"]),
            Entry("diary.choice.run-7", TravelDiaryFlavourCategory.ChoiceOutcome, "I rode hard and did not look back until the danger was thin behind me.", tags: ["choice", "run"]),
            Entry("diary.choice.run-8", TravelDiaryFlavourCategory.ChoiceOutcome, "I put daylight between us and called that a good answer.", tags: ["choice", "run"]),
            Entry("diary.choice.fight-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I stood my ground and made the rider respect the trail.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I answered hard and left no doubt that I was not backing down.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-3", TravelDiaryFlavourCategory.ChoiceOutcome, "I met the trouble head-on and kept my hands steady.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-4", TravelDiaryFlavourCategory.ChoiceOutcome, "I put up a fight and made every step cost him.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-5", TravelDiaryFlavourCategory.ChoiceOutcome, "I drew a line in the dust and held it.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-6", TravelDiaryFlavourCategory.ChoiceOutcome, "I fought like a man who had already decided to live through it.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-7", TravelDiaryFlavourCategory.ChoiceOutcome, "I gave as good as I got and made him think twice.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-8", TravelDiaryFlavourCategory.ChoiceOutcome, "I stayed put and let the day learn what stubborn meant.", tags: ["choice", "fight"]),
            Entry("diary.choice.bribe-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I paid my way through and let the problem ride off with my money.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I settled the matter with cash and kept the trail moving.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-3", TravelDiaryFlavourCategory.ChoiceOutcome, "I handed over the price of peace and hated it less than the alternative.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-4", TravelDiaryFlavourCategory.ChoiceOutcome, "I bought a little daylight and did not pretend it was free.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-5", TravelDiaryFlavourCategory.ChoiceOutcome, "I paid and kept my face straight while the money left.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-6", TravelDiaryFlavourCategory.ChoiceOutcome, "I let my wallet do the talking and called it mercy.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-7", TravelDiaryFlavourCategory.ChoiceOutcome, "I made the deal, lost the cash, and gained the road again.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-8", TravelDiaryFlavourCategory.ChoiceOutcome, "I spent the coin and kept the trouble from growing teeth.", tags: ["choice", "bribe"])
        ];

    private static TravelDiaryFlavourEntry[] BuildArrivalCompletionEntries()
        => [
            Entry("diary.arrival.completion-1", TravelDiaryFlavourCategory.ArrivalCompletion, "I reached town with dust on my boots and the trail behind me.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-2", TravelDiaryFlavourCategory.ArrivalCompletion, "I made it in with enough of the day left to call it a victory.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-3", TravelDiaryFlavourCategory.ArrivalCompletion, "I rolled into town with the journey finally out of my hands.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-4", TravelDiaryFlavourCategory.ArrivalCompletion, "I finished the road and let the town lights take over from there.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-5", TravelDiaryFlavourCategory.ArrivalCompletion, "I rode through the gate and felt the day finally let go.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-6", TravelDiaryFlavourCategory.ArrivalCompletion, "I came in tired, dusty, and glad to see a street that stayed put.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-7", TravelDiaryFlavourCategory.ArrivalCompletion, "I got to town before dark and counted that as a fair ending.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-8", TravelDiaryFlavourCategory.ArrivalCompletion, "I handed the trail back to memory and kept the town in front of me.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-9", TravelDiaryFlavourCategory.ArrivalCompletion, "I reached the end with my hat, my horse, and my temper still in one piece.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-10", TravelDiaryFlavourCategory.ArrivalCompletion, "I pulled into town and let the dust settle without me.", tags: ["arrival", "completion"])
        ];
}
