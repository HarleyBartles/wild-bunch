namespace WildBunch.Domain.Travel;

public enum JourneyTrailEventKind
{
    Lucky = 0,
    BadLuck = 1
}

public enum JourneyTrailEventId
{
    LuckyCoinCache = 0,
    LuckyFoodCache = 1,
    LuckyWaterSeep = 2,
    BadLuckWashout = 3,
    BadLuckFoodLoss = 4,
    BadLuckDustStorm = 5,
    BadLuckSpookedHorse = 6
}

public sealed record JourneyTrailEventState(
    JourneyTrailEventId Id,
    JourneyTrailEventKind Kind,
    string Title,
    string Message,
    decimal WalletDelta,
    int FoodDelta,
    int CanteenChargeDelta,
    int HorseHungerDelta,
    int HorseThirstDelta,
    int HorseExhaustionDelta,
    int DelayDays,
    int HeatIncrease)
{
    public static JourneyTrailEventState CreateLucky(
        JourneyTrailEventId id,
        string title,
        string message,
        decimal walletDelta = 0m,
        int foodDelta = 0,
        int canteenChargeDelta = 0)
        => new(
            id,
            JourneyTrailEventKind.Lucky,
            title,
            message,
            WalletDelta: walletDelta,
            FoodDelta: foodDelta,
            CanteenChargeDelta: canteenChargeDelta,
            HorseHungerDelta: 0,
            HorseThirstDelta: 0,
            HorseExhaustionDelta: 0,
            DelayDays: 0,
            HeatIncrease: 0);

    public static JourneyTrailEventState CreateBadLuck(
        JourneyTrailEventId id,
        string title,
        string message,
        int foodDelta = 0,
        int canteenChargeDelta = 0,
        int horseHungerDelta = 0,
        int horseThirstDelta = 0,
        int horseExhaustionDelta = 0,
        int delayDays = 0,
        int heatIncrease = 0)
        => new(
            id,
            JourneyTrailEventKind.BadLuck,
            title,
            message,
            WalletDelta: 0m,
            FoodDelta: foodDelta,
            CanteenChargeDelta: canteenChargeDelta,
            HorseHungerDelta: horseHungerDelta,
            HorseThirstDelta: horseThirstDelta,
            HorseExhaustionDelta: horseExhaustionDelta,
            DelayDays: delayDays,
            HeatIncrease: heatIncrease);
}
