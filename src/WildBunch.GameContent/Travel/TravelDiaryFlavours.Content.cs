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
            Entry("diary.day-opening.open-range-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with the open range laid out flat and patient before me.", tags: ["opening", "open-range"], terrain: TrailTerrain.OpenRange),
            Entry("diary.day-opening.open-range-2", TravelDiaryFlavourCategory.DayOpening, "I start the day with grass, dust, and a long view that does not care about my plans.", tags: ["opening", "open-range"], terrain: TrailTerrain.OpenRange),
            Entry("diary.day-opening.hills-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with the hills shouldering up ahead of me.", tags: ["opening", "hills"], terrain: TrailTerrain.Hills),
            Entry("diary.day-opening.hills-2", TravelDiaryFlavourCategory.DayOpening, "I start the day knowing the hills will take their toll one slope at a time.", tags: ["opening", "hills"], terrain: TrailTerrain.Hills),
            Entry("diary.day-opening.badlands-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with hard stone underfoot and badlands ahead.", tags: ["opening", "badlands"], terrain: TrailTerrain.Badlands),
            Entry("diary.day-opening.badlands-2", TravelDiaryFlavourCategory.DayOpening, "I start the day with the badlands looking mean about it and me trying not to argue.", tags: ["opening", "badlands"], terrain: TrailTerrain.Badlands),
            Entry("diary.day-opening.mountains-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with the mountains rising like a warning.", tags: ["opening", "mountains"], terrain: TrailTerrain.Mountains),
            Entry("diary.day-opening.general-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with my hat low and the trail already asking questions.", tags: ["opening", "general"])
        ];

    private static TravelDiaryFlavourEntry[] BuildQuietTextureEntries()
        => [
            Entry("diary.terrain.open-range-1", TravelDiaryFlavourCategory.QuietTexture, "The open range keeps me honest, and the wind does most of the talking.", tags: ["quiet", "open-range"], terrain: TrailTerrain.OpenRange),
            Entry("diary.terrain.open-range-2", TravelDiaryFlavourCategory.QuietTexture, "I ride through miles of grass and dust, with only hawk shadows for company.", tags: ["quiet", "open-range", "hawks"], terrain: TrailTerrain.OpenRange),
            Entry("diary.terrain.hills-1", TravelDiaryFlavourCategory.QuietTexture, "The hills break the trail into small hard victories.", tags: ["quiet", "hills"], terrain: TrailTerrain.Hills),
            Entry("diary.terrain.hills-2", TravelDiaryFlavourCategory.QuietTexture, "I pass through rolling country where every ridge hides the next one.", tags: ["quiet", "hills"], terrain: TrailTerrain.Hills),
            Entry("diary.terrain.badlands-1", TravelDiaryFlavourCategory.QuietTexture, "The badlands strip the world down to stone, glare, and dry silence.", tags: ["quiet", "badlands"], terrain: TrailTerrain.Badlands),
            Entry("diary.terrain.badlands-2", TravelDiaryFlavourCategory.QuietTexture, "Broken ground and sharp cut banks keep me watching my step.", tags: ["quiet", "badlands", "tracks"], terrain: TrailTerrain.Badlands),
            Entry("diary.terrain.mountains-1", TravelDiaryFlavourCategory.QuietTexture, "The mountain trail stays narrow and stubborn, like it resents every boot print.", tags: ["quiet", "mountains"], terrain: TrailTerrain.Mountains),
            Entry("diary.terrain.mountains-2", TravelDiaryFlavourCategory.QuietTexture, "The ridgelines keep the wind busy and the views too wide for comfort.", tags: ["quiet", "mountains", "wind"], terrain: TrailTerrain.Mountains),
            Entry("diary.terrain.campfire-1", TravelDiaryFlavourCategory.QuietTexture, "I pass a cold campfire and old tracks that still point the right way.", tags: ["quiet", "tracks", "camp"]),
            Entry("diary.terrain.peddler-1", TravelDiaryFlavourCategory.QuietTexture, "A peddler's wagon sits far off the trail, and I leave it to its own business.", tags: ["quiet", "peddler", "wagon"]),
            Entry("diary.terrain.ranch-1", TravelDiaryFlavourCategory.QuietTexture, "I ride past a broken fence and a ranch hand mending what the wind already tested.", tags: ["quiet", "ranch", "fence"]),
            Entry("diary.terrain.smoke-1", TravelDiaryFlavourCategory.QuietTexture, "Distant smoke hangs over a camp I never reach, and I keep to my own road.", tags: ["quiet", "smoke", "camp"])
        ];

    private static TravelDiaryFlavourEntry[] BuildLuckyEventEntries()
        => [
            Entry("diary.lucky.coin-cache-1", TravelDiaryFlavourCategory.LuckyEvent, "I find a little luck in the dust and tuck it away before the trail notices.", tags: ["lucky", "coin"]),
            Entry("diary.lucky.food-cache-1", TravelDiaryFlavourCategory.LuckyEvent, "I stumble onto a cache of trail grub and let myself grin about it.", tags: ["lucky", "food"]),
            Entry("diary.lucky.water-seep-1", TravelDiaryFlavourCategory.LuckyEvent, "I catch a hidden seep and feel the day loosen its grip on me.", tags: ["lucky", "water"]),
            Entry("diary.lucky-waypoint-1", TravelDiaryFlavourCategory.LuckyEvent, "I find the old track right where I needed it and save a hard detour.", tags: ["lucky", "trail"]),
            Entry("diary.lucky-shade-1", TravelDiaryFlavourCategory.LuckyEvent, "I find a strip of shade by the creek and count it as a kindness.", tags: ["lucky", "water", "shade"]),
            Entry("diary.lucky-scrap-1", TravelDiaryFlavourCategory.LuckyEvent, "A trader leaves behind a useful scrap, and I am quick enough to keep it.", tags: ["lucky", "trader"]),
            Entry("diary.lucky-spring-1", TravelDiaryFlavourCategory.LuckyEvent, "I come on a spring still holding water, and the day stops grinding so hard.", tags: ["lucky", "water", "spring"]),
            Entry("diary.lucky-marker-1", TravelDiaryFlavourCategory.LuckyEvent, "A weathered marker points me where I meant to go, which feels like a favor.", tags: ["lucky", "trail"]),
            Entry("diary.lucky-crossing-1", TravelDiaryFlavourCategory.LuckyEvent, "I find a clean crossing and save myself a mess of delay.", tags: ["lucky", "trail", "crossing"]),
            Entry("diary.lucky-coffee-1", TravelDiaryFlavourCategory.LuckyEvent, "I meet a traveling camp with enough coffee to share, and I do not waste the moment.", tags: ["lucky", "camp", "traveller"]),
            Entry("diary.lucky-wire-1", TravelDiaryFlavourCategory.LuckyEvent, "I spot a length of usable wire by the road and pocket it before the dust can object.", tags: ["lucky", "camp"]),
            Entry("diary.lucky-quiet-mile-1", TravelDiaryFlavourCategory.LuckyEvent, "I get one quiet mile where nothing asks for payment, and I take it.", tags: ["lucky", "mile"])
        ];

    private static TravelDiaryFlavourEntry[] BuildUnluckyEventEntries()
        => [
            Entry("diary.unlucky.washout-1", TravelDiaryFlavourCategory.UnluckyEvent, "A washout makes me work for every inch, and I do not get to complain about it.", tags: ["unlucky", "weather"]),
            Entry("diary.unlucky.food-loss-1", TravelDiaryFlavourCategory.UnluckyEvent, "A rough patch costs me supplies, and the trail keeps its poker face.", tags: ["unlucky", "food"]),
            Entry("diary.unlucky.spooked-horse-1", TravelDiaryFlavourCategory.UnluckyEvent, "My horse jumps at the wrong noise, and I spend the rest of the day paying for it.", tags: ["unlucky", "horse"], requiresHorsePresent: true),
            Entry("diary.unlucky.dust-storm-1", TravelDiaryFlavourCategory.UnluckyEvent, "The dust storm rolls through like it has a grudge against me.", tags: ["unlucky", "dust"]),
            Entry("diary.unlucky-wheel-1", TravelDiaryFlavourCategory.UnluckyEvent, "A wheel ruts hard and steals time I did not mean to spend.", tags: ["unlucky", "wagon"]),
            Entry("diary.unlucky-gully-1", TravelDiaryFlavourCategory.UnluckyEvent, "A gullied stretch of trail shakes loose my patience and some daylight.", tags: ["unlucky", "trail"]),
            Entry("diary.unlucky-vultures-1", TravelDiaryFlavourCategory.UnluckyEvent, "Vultures keep circling ground I would rather not investigate.", tags: ["unlucky", "vultures"]),
            Entry("diary.unlucky-fence-1", TravelDiaryFlavourCategory.UnluckyEvent, "A broken fence line and scattered brush slow me down more than they ought to.", tags: ["unlucky", "fence"]),
            Entry("diary.unlucky-rain-1", TravelDiaryFlavourCategory.UnluckyEvent, "Cold rain turns the trail slick, and every step feels longer than it should.", tags: ["unlucky", "weather"]),
            Entry("diary.unlucky-fork-1", TravelDiaryFlavourCategory.UnluckyEvent, "I take a wrong fork and burn daylight sorting out my mistake.", tags: ["unlucky", "trail"]),
            Entry("diary.unlucky-camp-1", TravelDiaryFlavourCategory.UnluckyEvent, "I come on an abandoned camp and find nothing there but the feeling of being late.", tags: ["unlucky", "camp"]),
            Entry("diary.unlucky-grit-1", TravelDiaryFlavourCategory.UnluckyEvent, "The wind throws grit in my face until the day feels spiteful.", tags: ["unlucky", "wind"])
        ];

    private static TravelDiaryFlavourEntry[] BuildFoeEncounterIntroEntries()
        => [
            Entry("diary.foe.intro-road-agent-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A road agent squares up ahead of me, and I keep my hand close.", tags: ["foe", "road-agent"]),
            Entry("diary.foe.intro-bandit-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A bandit shows himself early, which means the day just got meaner.", tags: ["foe", "bandit"]),
            Entry("diary.foe.intro-deserter-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A deserter cuts a hard line across the trail and leaves me no easy read.", tags: ["foe", "deserter"]),
            Entry("diary.foe.intro-deputy-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A crooked deputy rides in with a look I do not trust.", tags: ["foe", "crooked-deputy"]),
            Entry("diary.foe.intro-bounty-hunter-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A worn-out bounty hunter blocks the way, tired but still stubborn about it.", tags: ["foe", "bounty-hunter"]),
            Entry("diary.foe.intro-rider-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A hard-eyed rider cuts across my path, and I keep my hand close.", tags: ["foe", "hard-eyed-rider"]),
            Entry("diary.foe.intro-claim-jumper-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A claim jumper stands where he ought not be, which tells me plenty.", tags: ["foe", "claim-jumper"]),
            Entry("diary.foe.intro-cattle-thief-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A cattle thief drifts out of the brush and thinks about my road too long.", tags: ["foe", "cattle-thief"]),
            Entry("diary.foe.intro-drifter-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A desperate drifter stops pretending not to notice me.", tags: ["foe", "drifter"]),
            Entry("diary.foe.intro-hired-gun-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A hired gun rides in with a cold look and no wasted motion.", tags: ["foe", "hired-gun"]),
            Entry("diary.foe.intro-scout-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A suspicious scout blocks the trail, and I do not like the look of that pause.", tags: ["foe", "scout"]),
            Entry("diary.foe.intro-trail-blocker-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A trail blocker has picked a bad place to make a stand.", tags: ["foe", "trail-blocker"]),
            Entry("diary.foe.intro-rider-2", TravelDiaryFlavourCategory.FoeEncounterIntro, "A low-slung rider waits where the trail narrows, making the whole road feel smaller.", tags: ["foe", "rider"]),
            Entry("diary.foe.intro-stranger-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A sour-looking stranger turns his horse across my path and waits me out.", tags: ["foe", "stranger"]),
            Entry("diary.foe.intro-pair-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A pair of riders stop talking when they see me, which is answer enough.", tags: ["foe", "pair"]),
            Entry("diary.foe.intro-brush-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A man with a mean look and no hurry rides out of the brush.", tags: ["foe", "brush"])
        ];

    private static TravelDiaryFlavourEntry[] BuildResourceScarcityEntries()
        => [
            Entry("diary.resources.low-food-1", TravelDiaryFlavourCategory.ResourceScarcity, "My food is getting thin, and I count every bite like it matters.", tags: ["resource", "food"]),
            Entry("diary.resources.low-food-2", TravelDiaryFlavourCategory.ResourceScarcity, "I am down to the kind of provisions that make a man think ahead.", tags: ["resource", "food"]),
            Entry("diary.resources.low-food-3", TravelDiaryFlavourCategory.ResourceScarcity, "I measure supper before noon because I have to.", tags: ["resource", "food"]),
            Entry("diary.resources.low-food-4", TravelDiaryFlavourCategory.ResourceScarcity, "The trail keeps me honest about how little I have left to eat.", tags: ["resource", "food"]),
            Entry("diary.resources.low-feed-1", TravelDiaryFlavourCategory.ResourceScarcity, "My horse feed is running low, so I keep a close eye on the next stop.", tags: ["resource", "horse-feed"], requiresHorsePresent: true),
            Entry("diary.resources.low-feed-2", TravelDiaryFlavourCategory.ResourceScarcity, "I do not have much horse feed left, and the horse knows it before I say a word.", tags: ["resource", "horse-feed"], requiresHorsePresent: true),
            Entry("diary.resources.low-supplies-1", TravelDiaryFlavourCategory.ResourceScarcity, "My saddlebag is lighter than it ought to be, and I feel every missing ounce.", tags: ["resource", "supplies"]),
            Entry("diary.resources.low-supplies-2", TravelDiaryFlavourCategory.ResourceScarcity, "I keep thinking about the next town because my pack is not keeping up.", tags: ["resource", "supplies"]),
            Entry("diary.resources.low-general-1", TravelDiaryFlavourCategory.ResourceScarcity, "The trail has me watching food, feed, and time like a gambler watches a table.", tags: ["resource", "general"]),
            Entry("diary.resources.low-general-2", TravelDiaryFlavourCategory.ResourceScarcity, "I am guarding my last matches, my last mouthfuls, and my temper.", tags: ["resource", "general"])
        ];

    private static TravelDiaryFlavourEntry[] BuildWaterScarcityEntries()
        => [
            Entry("diary.water.dry-canteen-1", TravelDiaryFlavourCategory.WaterScarcity, "My canteen is light, and the trail is not handing out mercy.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-2", TravelDiaryFlavourCategory.WaterScarcity, "Every dry mile reminds me to keep the canteen close and my temper closer.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-3", TravelDiaryFlavourCategory.WaterScarcity, "I am watching water the way a prospector watches a last gold speck.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-4", TravelDiaryFlavourCategory.WaterScarcity, "The dry trail keeps asking questions my canteen cannot answer for long.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-5", TravelDiaryFlavourCategory.WaterScarcity, "I ride on hope and a shallow canteen, which is no way to stay cheerful.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
            Entry("diary.water.dry-canteen-6", TravelDiaryFlavourCategory.WaterScarcity, "The sun keeps asking for more water than I have to give.", tags: ["water", "dry"], requiresRouteWaterSecure: false)
        ];

    private static TravelDiaryFlavourEntry[] BuildWaterReliefEntries()
        => [
            Entry("diary.water.relief-1", TravelDiaryFlavourCategory.WaterRelief, "I get to breathe easier when the water holds and the canteen stops feeling so loud.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-2", TravelDiaryFlavourCategory.WaterRelief, "A good stretch of water makes the trail feel a little less personal.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-3", TravelDiaryFlavourCategory.WaterRelief, "I find enough water to keep my head straight and my pace honest.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-4", TravelDiaryFlavourCategory.WaterRelief, "The route water is holding, and that alone buys me some peace.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-5", TravelDiaryFlavourCategory.WaterRelief, "A spring gives me room to breathe, and I do not waste the chance.", tags: ["water", "relief", "spring"], requiresRouteWaterSecure: true),
            Entry("diary.water.relief-6", TravelDiaryFlavourCategory.WaterRelief, "The canteen stops rattling so hard once I know the next water is coming.", tags: ["water", "relief"], requiresRouteWaterSecure: true)
        ];

    private static TravelDiaryFlavourEntry[] BuildHorsePressureEntries()
        => [
            Entry("diary.horse.pressure-1", TravelDiaryFlavourCategory.HorsePressure, "My horse is feeling the miles, so I keep one eye on every step.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-2", TravelDiaryFlavourCategory.HorsePressure, "The horse is working hard enough to earn my attention, and I give it freely.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-3", TravelDiaryFlavourCategory.HorsePressure, "I can feel the horse start to wear down, and I do not like what that means for tomorrow.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-4", TravelDiaryFlavourCategory.HorsePressure, "The horse takes the strain, and I make a note to treat it better than I was treated.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-5", TravelDiaryFlavourCategory.HorsePressure, "My horse is blowing hard enough that I keep easing my hand.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-6", TravelDiaryFlavourCategory.HorsePressure, "I can feel the horse starting to lag, and I do not like the look of tomorrow.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-7", TravelDiaryFlavourCategory.HorsePressure, "The horse takes the climb badly, and I take the hint.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-8", TravelDiaryFlavourCategory.HorsePressure, "I keep checking the horse's step and pretending that counts as comfort.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-9", TravelDiaryFlavourCategory.HorsePressure, "The horse is tired enough that every mile feels borrowed.", tags: ["horse", "pressure"], requiresHorsePresent: true),
            Entry("diary.horse.pressure-10", TravelDiaryFlavourCategory.HorsePressure, "I slow the pace some, because pride does not feed a horse.", tags: ["horse", "pressure"], requiresHorsePresent: true)
        ];

    private static TravelDiaryFlavourEntry[] BuildChoiceOutcomeEntries()
        => [
            Entry("diary.choice.run-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I run for it and let the dust think it won.", tags: ["choice", "run"]),
            Entry("diary.choice.run-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I choose speed over pride and keep moving.", tags: ["choice", "run"]),
            Entry("diary.choice.run-3", TravelDiaryFlavourCategory.ChoiceOutcome, "I put distance between me and trouble before trouble can get clever.", tags: ["choice", "run"]),
            Entry("diary.choice.run-4", TravelDiaryFlavourCategory.ChoiceOutcome, "I take the wiser road and leave the hard questions behind.", tags: ["choice", "run"]),
            Entry("diary.choice.run-5", TravelDiaryFlavourCategory.ChoiceOutcome, "I give the trail my back and trust my legs.", tags: ["choice", "run"]),
            Entry("diary.choice.run-6", TravelDiaryFlavourCategory.ChoiceOutcome, "I slip away while there is still time to do it clean.", tags: ["choice", "run"]),
            Entry("diary.choice.run-7", TravelDiaryFlavourCategory.ChoiceOutcome, "I ride hard and do not look back until the danger is thin behind me.", tags: ["choice", "run"]),
            Entry("diary.choice.run-8", TravelDiaryFlavourCategory.ChoiceOutcome, "I put daylight between us and call that a good answer.", tags: ["choice", "run"]),
            Entry("diary.choice.fight-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I stand my ground and make the rider respect the trail.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I answer hard and leave no doubt that I am not backing down.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-3", TravelDiaryFlavourCategory.ChoiceOutcome, "I meet the trouble head-on and keep my hands steady.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-4", TravelDiaryFlavourCategory.ChoiceOutcome, "I put up a fight and make every step cost him.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-5", TravelDiaryFlavourCategory.ChoiceOutcome, "I draw a line in the dust and hold it.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-6", TravelDiaryFlavourCategory.ChoiceOutcome, "I fight like a man who has already decided to live through it.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-7", TravelDiaryFlavourCategory.ChoiceOutcome, "I give as good as I get and make him think twice.", tags: ["choice", "fight"]),
            Entry("diary.choice.fight-8", TravelDiaryFlavourCategory.ChoiceOutcome, "I stay put and let the day learn what stubborn means.", tags: ["choice", "fight"]),
            Entry("diary.choice.bribe-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I pay my way through and let the problem ride off with my money.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I settle the matter with cash and keep the trail moving.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-3", TravelDiaryFlavourCategory.ChoiceOutcome, "I hand over the price of peace and hate it less than the alternative.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-4", TravelDiaryFlavourCategory.ChoiceOutcome, "I buy a little daylight and do not pretend it is free.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-5", TravelDiaryFlavourCategory.ChoiceOutcome, "I pay and keep my face straight while the money leaves.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-6", TravelDiaryFlavourCategory.ChoiceOutcome, "I let my wallet do the talking and call it mercy.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-7", TravelDiaryFlavourCategory.ChoiceOutcome, "I make the deal, lose the cash, and gain the road again.", tags: ["choice", "bribe"]),
            Entry("diary.choice.bribe-8", TravelDiaryFlavourCategory.ChoiceOutcome, "I spend the coin and keep the trouble from growing teeth.", tags: ["choice", "bribe"])
        ];

    private static TravelDiaryFlavourEntry[] BuildArrivalCompletionEntries()
        => [
            Entry("diary.arrival.completion-1", TravelDiaryFlavourCategory.ArrivalCompletion, "I reach town with dust on my boots and the trail behind me.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-2", TravelDiaryFlavourCategory.ArrivalCompletion, "I make it in with enough of the day left to call it a victory.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-3", TravelDiaryFlavourCategory.ArrivalCompletion, "I roll into town with the journey finally out of my hands.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-4", TravelDiaryFlavourCategory.ArrivalCompletion, "I finish the road and let the town lights take over from here.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-5", TravelDiaryFlavourCategory.ArrivalCompletion, "I ride through the gate and feel the day finally let go.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-6", TravelDiaryFlavourCategory.ArrivalCompletion, "I come in tired, dusty, and glad to see a street that stays put.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-7", TravelDiaryFlavourCategory.ArrivalCompletion, "I get to town before dark and count that as a fair ending.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-8", TravelDiaryFlavourCategory.ArrivalCompletion, "I hand the trail back to memory and keep the town in front of me.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-9", TravelDiaryFlavourCategory.ArrivalCompletion, "I reach the end with my hat, my horse, and my temper still in one piece.", tags: ["arrival", "completion"]),
            Entry("diary.arrival.completion-10", TravelDiaryFlavourCategory.ArrivalCompletion, "I pull into town and let the dust settle without me.", tags: ["arrival", "completion"])
        ];
}
