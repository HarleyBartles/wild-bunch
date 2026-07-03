// src/WildBunch.Domain/World/TrailGeometry.cs
using System.Numerics;

namespace WildBunch.Domain.World;

public static class TrailGeometry
{
    public static double CalculatePixelDistance(Vector2 from, Vector2 to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static bool LinesIntersect(Vector2 line1From, Vector2 line1To, Vector2 line2From, Vector2 line2To)
    {
        // Using cross product to detect line segment intersection
        var d1 = Direction(line2From, line2To, line1From);
        var d2 = Direction(line2From, line2To, line1To);
        var d3 = Direction(line1From, line1To, line2From);
        var d4 = Direction(line1From, line1To, line2To);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }

        if (d1 == 0 && OnSegment(line2From, line2To, line1From)) return true;
        if (d2 == 0 && OnSegment(line2From, line2To, line1To)) return true;
        if (d3 == 0 && OnSegment(line1From, line1To, line2From)) return true;
        if (d4 == 0 && OnSegment(line1From, line1To, line2To)) return true;

        return false;
    }

    private static int Direction(Vector2 a, Vector2 b, Vector2 c)
    {
        var val = (b.Y - a.Y) * (c.X - a.X) - (b.X - a.X) * (c.Y - a.Y);
        if (val > 0) return 1;
        if (val < 0) return -1;
        return 0;
    }

    private static bool OnSegment(Vector2 a, Vector2 b, Vector2 c)
    {
        return c.X <= Math.Max(a.X, b.X) && c.X >= Math.Min(a.X, b.X) &&
               c.Y <= Math.Max(a.Y, b.Y) && c.Y >= Math.Min(a.Y, b.Y);
    }

    public static bool AreLinesParallel(Vector2 line1From, Vector2 line1To, Vector2 line2From, Vector2 line2To, double threshold = 0.1)
    {
        var dir1 = Vector2.Normalize(line1To - line1From);
        var dir2 = Vector2.Normalize(line2To - line2From);
        var dot = Math.Abs(Vector2.Dot(dir1, dir2));
        return dot > (1.0 - threshold);
    }
}
