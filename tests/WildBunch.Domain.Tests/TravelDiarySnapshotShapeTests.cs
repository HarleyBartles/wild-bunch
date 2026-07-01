using WildBunch.Domain.Travel;
using Xunit;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Falsification tests proving that BeatSlots is NOT a field on TravelDiaryDayState.
/// BeatSlots is a mapper-only projection derived from existing fields.
/// If someone added BeatSlots to TravelDiaryDayState, these tests would fail.
/// </summary>
public class TravelDiarySnapshotShapeTests
{
    [Fact]
    public void TravelDiaryDayState_HasNoBeatSlotsField()
    {
        var stateProperties = typeof(TravelDiaryDayState).GetProperties();
        Assert.DoesNotContain(stateProperties, p => p.Name.Contains("BeatSlot"));
    }
}
