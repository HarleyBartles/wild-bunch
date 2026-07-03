// src/WildBunch.GameContent/NewGame/RideDayCalculator.cs
namespace WildBunch.GameContent.NewGame;

public static class RideDayCalculator
{
    private const decimal MinDays = 2m;
    private const decimal MaxDays = 5m;
    private const decimal OutlierDays = 6m;

    public static decimal CalculateRideDays(
        TrailEdgeCandidate edge,
        double coordinateScale,
        int? outlierSlot)
    {
        // Check if this is an outlier trail
        if (outlierSlot.HasValue && (edge.FromSlot == outlierSlot.Value || edge.ToSlot == outlierSlot.Value))
        {
            return OutlierDays;
        }

        // Calculate ride days from pixel distance
        var rawRideDays = Math.Round(edge.PixelDistance / coordinateScale, 1);
        var clampedDistance = Math.Max(MinDays, Math.Min(MaxDays, (decimal)rawRideDays));
        
        return clampedDistance;
    }
}
