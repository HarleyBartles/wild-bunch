namespace WildBunch.GameContent.NewGame;

internal sealed record TrailEdge(int FromSlot, int ToSlot, double PixelDistance)
{
    public (int Low, int High) OrderedSlots
        => FromSlot <= ToSlot ? (FromSlot, ToSlot) : (ToSlot, FromSlot);
}
