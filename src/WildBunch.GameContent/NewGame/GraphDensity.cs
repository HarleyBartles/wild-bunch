namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Controls extra Delaunay edges beyond MST, encoded as 1 bit in seed codec.
/// </summary>
public enum GraphDensity
{
    Sparse = 0,
    Dense = 1
}
