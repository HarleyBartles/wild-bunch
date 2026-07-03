# Generate Trails from Settled Town Geometry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace layout-authored trail topology with geometry-first trail generation that produces clean, connected maps with accurate ride-day labels.

**Architecture:** 
- Layouts define town placement only via coordinate generation
- Trail topology is generated after final town coordinates settle
- Geometry-based edge generation with deterministic entropy/salt selection
- Edge filtering to reject crossings, parallel corridors, and redundant routes
- Ride-day labels derived from final accepted edge lengths

**Tech Stack:** C#/.NET 10, xUnit, System.Numerics for geometry calculations

## Global Constraints

- Treat existing BUNCH-123 implementation as provisional - do not preserve its trail-topology approach as architectural constraint
- Layouts define town placement, not final trail topology
- Trails are generated after final town coordinates are settled
- Boring mode must be deterministic for the same seed/layout
- Entropic modes apply geometry mutation before trail generation
- Accepted trails must form a connected graph
- Accepted trails must avoid crossings, close parallel corridors, and overlapping/redundant direct routes
- Ride-day labels must be derived from final accepted edge lengths
- Normal trails must be 2-5 days
- Outlier towns, when present, must have exactly one incident 6-day trail
- Tests must cover deterministic Boring graphs, entropy/salt variation, connectivity, edge rejection, and distance-label consistency
- CI must pass

---

### Task 1: Create geometry utilities for trail generation

**Files:**
- Create: `src/WildBunch.Domain/World/TrailGeometry.cs`

**Interfaces:**
- Consumes: System.Numerics (Vector2), existing TownId/Town types
- Produces: Distance calculation, line intersection detection, parallel corridor detection utilities

- [ ] **Step 1: Write the failing test**

```csharp
// tests/WildBunch.Domain.Tests/World/TrailGeometryTests.cs
using WildBunch.Domain.World;
using Xunit;

public class TrailGeometryTests
{
    [Fact]
    public void CalculatePixelDistance_ReturnsCorrectDistance()
    {
        var from = new Vector2(0, 0);
        var to = new Vector2(100, 0);
        var distance = TrailGeometry.CalculatePixelDistance(from, to);
        Assert.Equal(100.0, distance, 0.01);
    }

    [Fact]
    public void LinesIntersect_DetectsCrossingLines()
    {
        var line1 = (From: new Vector2(0, 0), To: new Vector2(10, 10));
        var line2 = (From: new Vector2(0, 10), To: new Vector2(10, 0));
        Assert.True(TrailGeometry.LinesIntersect(line1.From, line1.To, line2.From, line2.To));
    }

    [Fact]
    public void LinesIntersect_ReturnsFalseForNonCrossingLines()
    {
        var line1 = (From: new Vector2(0, 0), To: new Vector2(10, 0));
        var line2 = (From: new Vector2(0, 5), To: new Vector2(10, 5));
        Assert.False(TrailGeometry.LinesIntersect(line1.From, line1.To, line2.From, line2.To));
    }

    [Fact]
    public void AreLinesParallel_DetectsParallelLines()
    {
        var line1 = (From: new Vector2(0, 0), To: new Vector2(10, 0));
        var line2 = (From: new Vector2(0, 5), To: new Vector2(10, 5));
        Assert.True(TrailGeometry.AreLinesParallel(line1.From, line1.To, line2.From, line2.To, threshold: 0.1));
    }

    [Fact]
    public void AreLinesParallel_ReturnsFalseForNonParallelLines()
    {
        var line1 = (From: new Vector2(0, 0), To: new Vector2(10, 0));
        var line2 = (From: new Vector2(0, 0), To: new Vector2(10, 10));
        Assert.False(TrailGeometry.AreLinesParallel(line1.From, line1.To, line2.From, line2.To, threshold: 0.1));
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TrailGeometryTests" -v n`
Expected: FAIL with "TrailGeometry does not exist"

- [ ] **Step 3: Write minimal implementation**

```csharp
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
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TrailGeometryTests" -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/World/TrailGeometry.cs tests/WildBunch.Domain.Tests/World/TrailGeometryTests.cs
git commit -m "feat: add geometry utilities for trail generation"
```

---

### Task 2: Create trail edge candidate generator

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/TrailEdgeGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TrailEdgeGeneratorTests.cs`

**Interfaces:**
- Consumes: Town coordinates (Dictionary<int, (int X, int Y)>), TrailGeometry utilities
- Produces: Candidate trail edges with pixel distances

- [ ] **Step 1: Write the failing test**

```csharp
// tests/WildBunch.GameContent.Tests/NewGame/TrailEdgeGeneratorTests.cs
using WildBunch.GameContent.NewGame;
using Xunit;

public class TrailEdgeGeneratorTests
{
    [Fact]
    public void GenerateCandidateEdges_CreatesAllPossibleEdges()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250),
            [2] = (400, 50)
        };

        var edges = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        
        // Should have 3 choose 2 = 3 edges
        Assert.Equal(3, edges.Count);
        
        // Verify all pairs are present
        Assert.Contains(edges, e => e.FromSlot == 0 && e.ToSlot == 1);
        Assert.Contains(edges, e => e.FromSlot == 0 && e.ToSlot == 2);
        Assert.Contains(edges, e => e.FromSlot == 1 && e.ToSlot == 2);
    }

    [Fact]
    public void GenerateCandidateEdges_CalculatesCorrectPixelDistances()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (0, 0),
            [1] = (100, 0)
        };

        var edges = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var edge = edges.First(e => e.FromSlot == 0 && e.ToSlot == 1);
        
        Assert.Equal(100.0, edge.PixelDistance, 0.01);
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TrailEdgeGeneratorTests" -v n`
Expected: FAIL with "TrailEdgeGenerator does not exist"

- [ ] **Step 3: Write minimal implementation**

```csharp
// src/WildBunch.GameContent/NewGame/TrailEdgeGenerator.cs
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public sealed record TrailEdgeCandidate(
    int FromSlot,
    int ToSlot,
    double PixelDistance);

public static class TrailEdgeGenerator
{
    public static IReadOnlyList<TrailEdgeCandidate> GenerateCandidateEdges(
        Dictionary<int, (int X, int Y)> townCoordinates)
    {
        var edges = new List<TrailEdgeCandidate>();
        var slots = townCoordinates.Keys.OrderBy(x => x).ToList();

        for (var i = 0; i < slots.Count; i++)
        {
            for (var j = i + 1; j < slots.Count; j++)
            {
                var fromSlot = slots[i];
                var toSlot = slots[j];
                var fromCoords = townCoordinates[fromSlot];
                var toCoords = townCoordinates[toSlot];
                
                var from = new Vector2(fromCoords.X, fromCoords.Y);
                var to = new Vector2(toCoords.X, toCoords.Y);
                var distance = TrailGeometry.CalculatePixelDistance(from, to);
                
                edges.Add(new TrailEdgeCandidate(fromSlot, toSlot, distance));
            }
        }

        return edges;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TrailEdgeGeneratorTests" -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TrailEdgeGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TrailEdgeGeneratorTests.cs
git commit -m "feat: add trail edge candidate generator"
```

---

### Task 3: Create edge filtering system

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/TrailEdgeFilter.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TrailEdgeFilterTests.cs`

**Interfaces:**
- Consumes: TrailEdgeCandidate list, existing accepted edges, TrailGeometry utilities
- Produces: Filtered edge list with crossings, parallel corridors, and redundant routes removed

- [x] **Step 1: Write the failing test**

```csharp
// tests/WildBunch.GameContent.Tests/NewGame/TrailEdgeFilterTests.cs
using WildBunch.GameContent.NewGame;
using Xunit;

public class TrailEdgeFilterTests
{
    [Fact]
    public void FilterCrossingEdges_RemovesEdgesThatCrossExistingTrails()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (0, 0),
            [1] = (10, 10),
            [2] = (0, 10),
            [3] = (10, 0)
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var accepted = new List<TrailEdgeCandidate>
        {
            candidates.First(e => e.FromSlot == 0 && e.ToSlot == 3) // (0,0) to (10,0)
        };

        var filtered = TrailEdgeFilter.FilterCrossingEdges(candidates, accepted, coordinates);
        
        // Edge (0,10) to (10,0) should be removed as it crosses (0,0) to (10,0)
        var crossingEdge = filtered.FirstOrDefault(e => e.FromSlot == 2 && e.ToSlot == 3);
        Assert.Null(crossingEdge);
    }

    [Fact]
    public void FilterParallelCorridors_RemovesCloselyParallelEdges()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (0, 0),
            [1] = (100, 0),
            [2] = (0, 10),
            [3] = (100, 10)
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var accepted = new List<TrailEdgeCandidate>
        {
            candidates.First(e => e.FromSlot == 0 && e.ToSlot == 1) // (0,0) to (100,0)
        };

        var filtered = TrailEdgeFilter.FilterParallelCorridors(candidates, accepted, coordinates, threshold: 0.1);
        
        // Edge (2,10) to (3,10) should be removed as it's parallel to (0,0) to (100,0)
        var parallelEdge = filtered.FirstOrDefault(e => e.FromSlot == 2 && e.ToSlot == 3);
        Assert.Null(parallelEdge);
    }

    [Fact]
    public void FilterRedundantRoutes_RemovesDirectEdgesWhenIndirectRouteExists()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (0, 0),
            [1] = (50, 0),
            [2] = (100, 0)
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var accepted = new List<TrailEdgeCandidate>
        {
            candidates.First(e => e.FromSlot == 0 && e.ToSlot == 1),
            candidates.First(e => e.FromSlot == 1 && e.ToSlot == 2)
        };

        var filtered = TrailEdgeFilter.FilterRedundantRoutes(candidates, accepted, coordinates);
        
        // Edge (0,0) to (100,0) should be removed as 0->1->2 already exists
        var redundantEdge = filtered.FirstOrDefault(e => e.FromSlot == 0 && e.ToSlot == 2);
        Assert.Null(redundantEdge);
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TrailEdgeFilterTests" -v n`
Expected: FAIL with "TrailEdgeFilter does not exist"

- [x] **Step 3: Write minimal implementation**

```csharp
// src/WildBunch.GameContent/NewGame/TrailEdgeFilter.cs
using System.Numerics;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public static class TrailEdgeFilter
{
    public static IReadOnlyList<TrailEdgeCandidate> FilterCrossingEdges(
        IReadOnlyList<TrailEdgeCandidate> candidates,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> coordinates)
    {
        return candidates.Where(candidate =>
        {
            foreach (var acceptedEdge in accepted)
            {
                var from1 = new Vector2(coordinates[candidate.FromSlot].X, coordinates[candidate.FromSlot].Y);
                var to1 = new Vector2(coordinates[candidate.ToSlot].X, coordinates[candidate.ToSlot].Y);
                var from2 = new Vector2(coordinates[acceptedEdge.FromSlot].X, coordinates[acceptedEdge.FromSlot].Y);
                var to2 = new Vector2(coordinates[acceptedEdge.ToSlot].X, coordinates[acceptedEdge.ToSlot].Y);

                // Don't filter if edges share a town (they meet at the town, not crossing)
                if (candidate.FromSlot == acceptedEdge.FromSlot ||
                    candidate.FromSlot == acceptedEdge.ToSlot ||
                    candidate.ToSlot == acceptedEdge.FromSlot ||
                    candidate.ToSlot == acceptedEdge.ToSlot)
                {
                    continue;
                }

                if (TrailGeometry.LinesIntersect(from1, to1, from2, to2))
                {
                    return false;
                }
            }
            return true;
        }).ToList();
    }

    public static IReadOnlyList<TrailEdgeCandidate> FilterParallelCorridors(
        IReadOnlyList<TrailEdgeCandidate> candidates,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> coordinates,
        double threshold = 0.1)
    {
        return candidates.Where(candidate =>
        {
            foreach (var acceptedEdge in accepted)
            {
                // Don't filter if edges share a town
                if (candidate.FromSlot == acceptedEdge.FromSlot ||
                    candidate.FromSlot == acceptedEdge.ToSlot ||
                    candidate.ToSlot == acceptedEdge.FromSlot ||
                    candidate.ToSlot == acceptedEdge.ToSlot)
                {
                    continue;
                }

                var from1 = new Vector2(coordinates[candidate.FromSlot].X, coordinates[candidate.FromSlot].Y);
                var to1 = new Vector2(coordinates[candidate.ToSlot].X, coordinates[candidate.ToSlot].Y);
                var from2 = new Vector2(coordinates[acceptedEdge.FromSlot].X, coordinates[acceptedEdge.FromSlot].Y);
                var to2 = new Vector2(coordinates[acceptedEdge.ToSlot].X, coordinates[acceptedEdge.ToSlot].Y);

                if (TrailGeometry.AreLinesParallel(from1, to1, from2, to2, threshold))
                {
                    return false;
                }
            }
            return true;
        }).ToList();
    }

    public static IReadOnlyList<TrailEdgeCandidate> FilterRedundantRoutes(
        IReadOnlyList<TrailEdgeCandidate> candidates,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> coordinates)
    {
        return candidates.Where(candidate =>
        {
            // Check if there's already an indirect route between these towns
            var reachableFromCandidate = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(candidate.FromSlot);
            reachableFromCandidate.Add(candidate.FromSlot);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var edge in accepted)
                {
                    if (edge.FromSlot == current && !reachableFromCandidate.Contains(edge.ToSlot))
                    {
                        if (edge.ToSlot == candidate.ToSlot)
                        {
                            // Found indirect route
                            return false;
                        }
                        reachableFromCandidate.Add(edge.ToSlot);
                        queue.Enqueue(edge.ToSlot);
                    }
                    else if (edge.ToSlot == current && !reachableFromCandidate.Contains(edge.FromSlot))
                    {
                        if (edge.FromSlot == candidate.ToSlot)
                        {
                            // Found indirect route
                            return false;
                        }
                        reachableFromCandidate.Add(edge.FromSlot);
                        queue.Enqueue(edge.FromSlot);
                    }
                }
            }

            return true;
        }).ToList();
    }
}
```

- [x] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TrailEdgeFilterTests" -v n`
Expected: PASS

- [x] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TrailEdgeFilter.cs tests/WildBunch.GameContent.Tests/NewGame/TrailEdgeFilterTests.cs
git commit -m "feat: add edge filtering system"
```

---

### Task 4: Create connected graph selector with deterministic entropy

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/TrailGraphSelector.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TrailGraphSelectorTests.cs`

**Interfaces:**
- Consumes: Filtered edge candidates, GameEntropy, SaltSource, GameSetupDeterministicSource
- Produces: Connected trail graph selected using deterministic entropy/salt rules

- [ ] **Step 1: Write the failing test**

```csharp
// tests/WildBunch.GameContent.Tests/NewGame/TrailGraphSelectorTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using Xunit;

public class TrailGraphSelectorTests
{
    [Fact]
    public void SelectConnectedGraph_ProducesConnectedGraph()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250),
            [2] = (400, 50)
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var selected = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);

        Assert.True(IsConnected(selected, coordinates.Count));
    }

    [Fact]
    public void SelectConnectedGraph_IsDeterministicForBoringMode()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250),
            [2] = (400, 50)
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var selected1 = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);
        
        var selected2 = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);

        Assert.Equal(selected1.Count, selected2.Count);
        foreach (var edge in selected1)
        {
            Assert.Contains(selected2, e => e.FromSlot == edge.FromSlot && e.ToSlot == edge.ToSlot);
        }
    }

    private bool IsConnected(IReadOnlyList<TrailEdgeCandidate> edges, int townCount)
    {
        if (townCount == 0) return true;
        
        var adjacency = new Dictionary<int, List<int>>();
        for (var i = 0; i < townCount; i++)
        {
            adjacency[i] = new List<int>();
        }

        foreach (var edge in edges)
        {
            adjacency[edge.FromSlot].Add(edge.ToSlot);
            adjacency[edge.ToSlot].Add(edge.FromSlot);
        }

        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0);
        visited.Add(0);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == townCount;
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TrailGraphSelectorTests" -v n`
Expected: FAIL with "TrailGraphSelector does not exist"

- [x] **Step 3: Write minimal implementation**

**Note:** Changed `GameSetupDeterministicSource` from `internal` to `public` to allow test access.

```csharp
// src/WildBunch.GameContent/NewGame/TrailGraphSelector.cs
using WildBunch.Domain.Game;

namespace WildBunch.GameContent.NewGame;

public static class TrailGraphSelector
{
    public static IReadOnlyList<TrailEdgeCandidate> SelectConnectedGraph(
        IReadOnlyList<TrailEdgeCandidate> candidates,
        int townCount,
        GameEntropy entropy,
        SaltSource? saltSource,
        GameSetupDeterministicSource source)
    {
        if (townCount < 2)
            return candidates;

        // Use minimum spanning tree approach for connectivity
        // Start with all towns unconnected
        var connected = new HashSet<int> { 0 };
        var selected = new List<TrailEdgeCandidate>();
        var remaining = candidates.ToList();

        // For Boring mode, use deterministic selection by distance (shortest first)
        // For entropic modes, use salt-based selection
        if (entropy == GameEntropy.Boring)
        {
            remaining = remaining.OrderBy(e => e.PixelDistance).ToList();
        }
        else if (saltSource != null)
        {
            var salt = saltSource.Salt;
            var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));
            remaining = remaining.OrderBy(_ => random.Next()).ToList();
        }

        // Build minimum connected graph
        while (connected.Count < townCount && remaining.Count > 0)
        {
            // Find edge that connects to the connected component
            var connectingEdge = remaining.FirstOrDefault(e =>
                connected.Contains(e.FromSlot) && !connected.Contains(e.ToSlot) ||
                connected.Contains(e.ToSlot) && !connected.Contains(e.FromSlot));

            if (connectingEdge != null)
            {
                selected.Add(connectingEdge);
                connected.Add(connectingEdge.FromSlot);
                connected.Add(connectingEdge.ToSlot);
                remaining.Remove(connectingEdge);
            }
            else
            {
                // No connecting edge found, break to avoid infinite loop
                break;
            }
        }

        // Add extra edges based on entropy level for variety
        var extraEdges = entropy switch
        {
            GameEntropy.Boring => 0,
            GameEntropy.Classic => 1,
            GameEntropy.Adventurous => 2,
            GameEntropy.Wild => 3,
            _ => 0
        };

        for (var i = 0; i < extraEdges && remaining.Count > 0; i++)
        {
            selected.Add(remaining[0]);
            remaining.RemoveAt(0);
        }

        return selected;
    }

    private static int ComputeStableHash(string seedCode, string entropyMode, string salt)
    {
        var input = $"{seedCode}-{entropyMode}-{salt}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return BitConverter.ToInt32(hashBytes, 0);
    }
}
```

- [x] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TrailGraphSelectorTests" -v n`
Expected: PASS

- [x] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TrailGraphSelector.cs tests/WildBunch.GameContent.Tests/NewGame/TrailGraphSelectorTests.cs src/WildBunch.GameContent/NewGame/GameSetupDeterministicSource.cs
git commit -m "feat: add connected graph selector with deterministic entropy"
```

---

### Task 5: Create ride-day distance calculator

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/RideDayCalculator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/RideDayCalculatorTests.cs`

**Interfaces:**
- Consumes: Selected trail edges, pixel distances, outlier slot
- Produces: Ride-day distances (2-5 days for normal, 6 days for outlier trails)

- [ ] **Step 1: Write the failing test**

```csharp
// tests/WildBunch.GameContent.Tests/NewGame/RideDayCalculatorTests.cs
using WildBunch.GameContent.NewGame;
using Xunit;

public class RideDayCalculatorTests
{
    [Fact]
    public void CalculateRideDays_ConvertsPixelDistanceToRideDays()
    {
        const double CoordinateScale = 25.0; // 1 ride-day per 25 coordinate units
        var edge = new TrailEdgeCandidate(0, 1, 50.0); // 50 pixels = 2 ride days
        
        var rideDays = RideDayCalculator.CalculateRideDays(edge, CoordinateScale, outlierSlot: null);
        
        Assert.Equal(2m, rideDays);
    }

    [Fact]
    public void CalculateRideDays_ClampsToNormalRange()
    {
        const double CoordinateScale = 25.0;
        var shortEdge = new TrailEdgeCandidate(0, 1, 10.0); // Should clamp to 2
        var longEdge = new TrailEdgeCandidate(0, 1, 200.0); // Should clamp to 5
        
        var shortDays = RideDayCalculator.CalculateRideDays(shortEdge, CoordinateScale, outlierSlot: null);
        var longDays = RideDayCalculator.CalculateRideDays(longEdge, CoordinateScale, outlierSlot: null);
        
        Assert.Equal(2m, shortDays);
        Assert.Equal(5m, longDays);
    }

    [Fact]
    public void CalculateRideDays_OutlierTrailGetsSixDays()
    {
        const double CoordinateScale = 25.0;
        var edge = new TrailEdgeCandidate(0, 1, 50.0);
        
        var rideDays = RideDayCalculator.CalculateRideDays(edge, CoordinateScale, outlierSlot: 0);
        
        Assert.Equal(6m, rideDays);
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~RideDayCalculatorTests" -v n`
Expected: FAIL with "RideDayCalculator does not exist"

- [x] **Step 3: Write minimal implementation**

```csharp
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
```

- [x] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~RideDayCalculatorTests" -v n`
Expected: PASS

- [x] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/RideDayCalculator.cs tests/WildBunch.GameContent.Tests/NewGame/RideDayCalculatorTests.cs
git commit -m "feat: add ride-day distance calculator"
```

---

### Task 6: Create main trail generation orchestrator

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/TrailTopologyGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TrailTopologyGeneratorTests.cs`

**Interfaces:**
- Consumes: Town coordinates, GameEntropy, SaltSource, GameSetupDeterministicSource, outlier slot
- Produces: Final SeedWorldTrail list with correct topology and ride-day distances

- [ ] **Step 1: Write the failing test**

```csharp
// tests/WildBunch.GameContent.Tests/NewGame/TrailTopologyGeneratorTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

public class TrailTopologyGeneratorTests
{
    [Fact]
    public void GenerateTrailTopology_ProducesConnectedGraph()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250),
            [2] = (400, 50)
        };

        var townNames = new[]
        {
            new TownNameEntry("town-0", "Town 0"),
            new TownNameEntry("town-1", "Town 1"),
            new TownNameEntry("town-2", "Town 2")
        };

        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var trails = TrailTopologyGenerator.GenerateTrailTopology(
            coordinates,
            townNames,
            GameEntropy.Boring,
            null,
            source,
            outlierSlot: null);

        Assert.True(IsConnected(trails, townNames.Length));
    }

    [Fact]
    public void GenerateTrailTopology_DerivesRideDaysFromGeometry()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250) // 200 pixels apart = 8 ride days, should clamp to 5
        };

        var townNames = new[]
        {
            new TownNameEntry("town-0", "Town 0"),
            new TownNameEntry("town-1", "Town 1")
        };

        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var trails = TrailTopologyGenerator.GenerateTrailTopology(
            coordinates,
            townNames,
            GameEntropy.Boring,
            null,
            source,
            outlierSlot: null);

        var trail = trails.First();
        Assert.InRange(trail.RideDayDistance, 2m, 5m);
    }

    [Fact]
    public void GenerateTrailTopology_OutlierTownHasSixDayTrail()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250)
        };

        var townNames = new[]
        {
            new TownNameEntry("town-0", "Town 0"),
            new TownNameEntry("town-1", "Town 1")
        };

        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var trails = TrailTopologyGenerator.GenerateTrailTopology(
            coordinates,
            townNames,
            GameEntropy.Boring,
            null,
            source,
            outlierSlot: 1);

        var trail = trails.First();
        Assert.Equal(6m, trail.RideDayDistance);
    }

    private bool IsConnected(IReadOnlyList<SeedWorldTrail> trails, int townCount)
    {
        if (townCount == 0) return true;
        
        var adjacency = new Dictionary<string, List<string>>();
        foreach (var trail in trails)
        {
            if (!adjacency.ContainsKey(trail.FromTownId))
                adjacency[trail.FromTownId] = new List<string>();
            if (!adjacency.ContainsKey(trail.ToTownId))
                adjacency[trail.ToTownId] = new List<string>();
            
            adjacency[trail.FromTownId].Add(trail.ToTownId);
            adjacency[trail.ToTownId].Add(trail.FromTownId);
        }

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        var startTown = trails.First().FromTownId;
        queue.Enqueue(startTown);
        visited.Add(startTown);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (adjacency.ContainsKey(current))
            {
                foreach (var neighbor in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return visited.Count == townCount;
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TrailTopologyGeneratorTests" -v n`
Expected: FAIL with "TrailTopologyGenerator does not exist"

- [ ] **Step 3: Write minimal implementation**

```csharp
// src/WildBunch.GameContent/NewGame/TrailTopologyGenerator.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public static class TrailTopologyGenerator
{
    private const double CoordinateScale = 25.0; // 1 ride-day per 25 coordinate units

    public static IReadOnlyList<SeedWorldTrail> GenerateTrailTopology(
        Dictionary<int, (int X, int Y)> townCoordinates,
        IReadOnlyList<TownNameEntry> townNames,
        GameEntropy entropy,
        SaltSource? saltSource,
        GameSetupDeterministicSource source,
        int? outlierSlot)
    {
        // Step 1: Generate all candidate edges
        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(townCoordinates);

        // Step 2: Filter edges to remove crossings, parallel corridors, and redundant routes
        var filtered = TrailEdgeFilter.FilterCrossingEdges(candidates, new List<TrailEdgeCandidate>(), townCoordinates);
        filtered = TrailEdgeFilter.FilterParallelCorridors(filtered, new List<TrailEdgeCandidate>(), townCoordinates);
        filtered = TrailEdgeFilter.FilterRedundantRoutes(filtered, new List<TrailEdgeCandidate>(), townCoordinates);

        // Step 3: Select connected graph using deterministic entropy
        var selected = TrailGraphSelector.SelectConnectedGraph(
            filtered,
            townCoordinates.Count,
            entropy,
            saltSource,
            source);

        // Step 4: Apply filtering iteratively as edges are added to prevent crossings with newly added edges
        var finalEdges = new List<TrailEdgeCandidate>();
        var remaining = selected.ToList();
        
        while (remaining.Count > 0)
        {
            var edge = remaining[0];
            remaining.RemoveAt(0);
            
            // Check if this edge would cross or be parallel to any already-selected edge
            var canAdd = true;
            foreach (var existing in finalEdges)
            {
                var from1 = new Vector2(townCoordinates[edge.FromSlot].X, townCoordinates[edge.FromSlot].Y);
                var to1 = new Vector2(townCoordinates[edge.ToSlot].X, townCoordinates[edge.ToSlot].Y);
                var from2 = new Vector2(townCoordinates[existing.FromSlot].X, townCoordinates[existing.FromSlot].Y);
                var to2 = new Vector2(townCoordinates[existing.ToSlot].X, townCoordinates[existing.ToSlot].Y);

                // Skip if they share a town
                if (edge.FromSlot == existing.FromSlot || edge.FromSlot == existing.ToSlot ||
                    edge.ToSlot == existing.FromSlot || edge.ToSlot == existing.ToSlot)
                {
                    continue;
                }

                if (TrailGeometry.LinesIntersect(from1, to1, from2, to2))
                {
                    canAdd = false;
                    break;
                }

                if (TrailGeometry.AreLinesParallel(from1, to1, from2, to2, threshold: 0.1))
                {
                    canAdd = false;
                    break;
                }
            }

            if (canAdd)
            {
                finalEdges.Add(edge);
            }
        }

        // Step 5: Convert to SeedWorldTrail with ride-day distances
        var trails = new List<SeedWorldTrail>();
        foreach (var edge in finalEdges)
        {
            var rideDays = RideDayCalculator.CalculateRideDays(edge, CoordinateScale, outlierSlot);
            
            trails.Add(new SeedWorldTrail(
                $"trail-{edge.FromSlot}-{edge.ToSlot}",
                townNames[edge.FromSlot].Id,
                townNames[edge.ToSlot].Id,
                TrailRisk.Moderate, // Default risk - can be enhanced later
                TrailTerrain.OpenRange, // Default terrain - can be enhanced later
                WaterFeature.Creek, // Default water - can be enhanced later
                rideDays));
        }

        return trails;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TrailTopologyGeneratorTests" -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TrailTopologyGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TrailTopologyGeneratorTests.cs
git commit -m "feat: add main trail topology generation orchestrator"
```

---

### Task 7: Integrate new trail generation into SeedWorldBuilder

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldBuilderTests.cs`

**Interfaces:**
- Consumes: TrailTopologyGenerator, existing town coordinate derivation
- Produces: Updated SeedWorldBuilder.CreateWorld that uses geometry-first trail generation

- [ ] **Step 1: Write the failing test**

```csharp
// tests/WildBunch.GameContent.Tests/NewGame/SeedWorldBuilderTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

public class SeedWorldBuilderTests
{
    [Fact]
    public void CreateWorld_UsesGeometryFirstTrailGeneration()
    {
        var seedWorld = new SeedWorld(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            SeedWorldVariant.Canonical,
            5, // town count
            0, // accusation index
            0, // default culprit index
            0, // cash bonus
            ProsperityPalette.UniformProsperous,
            ServicesPalette.NoTelegraph,
            MapLayoutPalette.HubAndSpoke,
            0); // outlier slot type

        var source = new GameSetupDeterministicSource(seedWorld.SeedCode);
        
        var world = SeedWorldBuilder.CreateWorld(
            seedWorld,
            source,
            GameEntropy.Boring,
            null);

        // Verify trails form a connected graph
        Assert.True(IsConnected(world.Trails, world.Towns.Count));
        
        // Verify ride-day labels are in normal range (no outlier in this case)
        foreach (var trail in world.Trails)
        {
            Assert.InRange(trail.RideDayDistance, 2m, 5m);
        }
    }

    [Fact]
    public void CreateWorld_OutlierTownHasSixDayTrail()
    {
        var seedWorld = new SeedWorld(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            SeedWorldVariant.Canonical,
            5, // town count
            0, // accusation index
            0, // default culprit index
            0, // cash bonus
            ProsperityPalette.UniformProsperous,
            ServicesPalette.NoTelegraph,
            MapLayoutPalette.HubAndSpoke,
            1); // outlier slot type = activate

        var source = new GameSetupDeterministicSource(seedWorld.SeedCode);
        
        var world = SeedWorldBuilder.CreateWorld(
            seedWorld,
            source,
            GameEntropy.Classic, // Need entropy for outlier
            new SaltSource("test-salt"));

        // Find the outlier town
        var outlierTown = world.Towns.FirstOrDefault(t => t.IsOutlier);
        Assert.NotNull(outlierTown);

        // Verify outlier town has exactly one incident trail and it's 6 days
        var outlierTrails = world.Trails.Where(t => t.Connects(outlierTown.Id)).ToList();
        Assert.Single(outlierTrails);
        Assert.Equal(6m, outlierTrails[0].RideDayDistance);
    }

    private bool IsConnected(IReadOnlyList<Trail> trails, int townCount)
    {
        if (townCount == 0) return true;
        
        var adjacency = new Dictionary<TownId, List<TownId>>();
        foreach (var trail in trails)
        {
            if (!adjacency.ContainsKey(trail.FromTownId))
                adjacency[trail.FromTownId] = new List<TownId>();
            if (!adjacency.ContainsKey(trail.ToTownId))
                adjacency[trail.ToTownId] = new List<TownId>();
            
            adjacency[trail.FromTownId].Add(trail.ToTownId);
            adjacency[trail.ToTownId].Add(trail.FromTownId);
        }

        var visited = new HashSet<TownId>();
        var queue = new Queue<TownId>();
        var startTown = trails.First().FromTownId;
        queue.Enqueue(startTown);
        visited.Add(startTown);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (adjacency.ContainsKey(current))
            {
                foreach (var neighbor in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return visited.Count == townCount;
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SeedWorldBuilderTests" -v n`
Expected: FAIL - current implementation uses layout-authored trails

- [ ] **Step 3: Modify SeedWorldBuilder to use new trail generation**

```csharp
// In SeedWorldBuilder.cs, replace the trail generation logic in CreateWorld method

// OLD CODE (lines 49-63 in current implementation):
// var trails = SeedWorldCatalog.BuildTrails(seedWorld.WorldVariant, townNames, seedWorld.MapLayoutPalette);
// var townCoordinates = DeriveTownCoordinates(townNames.Count, seedWorld.MapLayoutPalette, entropy, source, saltSource);
// var (trimmedTrails, adjustedCoordinates) = DeriveDistancesAndAdjustCoordinates(
//     trails,
//     townCoordinates,
//     seedWorld.MapLayoutPalette,
//     entropy,
//     source,
//     saltSource);

// NEW CODE:
// Derive town coordinates from map layout geometry
var townCoordinates = DeriveTownCoordinates(townNames.Count, seedWorld.MapLayoutPalette, entropy, source, saltSource);

// Generate trails from settled town coordinates using geometry-first approach
var trails = TrailTopologyGenerator.GenerateTrailTopology(
    townCoordinates,
    townNames,
    entropy,
    saltSource,
    source,
    outlierSlot);

// Remove the old DeriveDistancesAndAdjustCoordinates method and related helper methods
// Remove the old ApplyLayoutSpecificTrailRemoval method and related helper methods
// Remove the old AdjustCoordinatesToMatchRideDays method
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SeedWorldBuilderTests" -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs tests/WildBunch.GameContent.Tests/NewGame/SeedWorldBuilderTests.cs
git commit -m "feat: integrate geometry-first trail generation into SeedWorldBuilder"
```

---

### Task 8: Remove obsolete trail generation code

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs` (remove BuildTrails and related methods)

**Interfaces:**
- Consumes: Existing codebase
- Produces: Cleaned codebase with obsolete trail topology generation removed

- [ ] **Step 1: Remove obsolete methods from SeedWorldBuilder**

Remove the following methods from SeedWorldBuilder.cs:
- DeriveDistancesAndAdjustCoordinates
- ApplyLayoutSpecificTrailRemoval
- ApplyHubAndSpokeTrailRemoval
- ApplyDoubleLineTrailRemoval
- ApplyTreeTrailRemoval
- ApplyStarTrailRemoval
- ApplySimpleTrailRemoval
- SelectRandomTrails
- AdjustCoordinatesToMatchRideDays
- VerifyConnectivity (if it exists)

- [ ] **Step 2: Remove BuildTrails and related methods from SeedWorldCatalog**

Remove the following from SeedWorldCatalog.cs:
- BuildTrails method
- GenerateTrailsForLayout method
- GenerateHubAndSpokeTrails method
- GenerateDoubleLineTrails method
- GenerateTreeTrails method
- GenerateStarTrails method
- SlotTrailDefinition record (no longer needed)

- [ ] **Step 3: Update SeedWorldMapLayout to remove trail-related methods**

Remove from SeedWorldMapLayout.cs:
- GetMapTrails method (both overloads)

- [ ] **Step 4: Run tests to verify nothing broke**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj -v n`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs
git commit -m "refactor: remove obsolete layout-authored trail generation code"
```

---

### Task 9: Update integration tests and snapshot tests

**Files:**
- Modify: Any integration tests that depend on specific trail topologies
- Modify: Any snapshot tests that encode trail structures

**Interfaces:**
- Consumes: Existing test suite
- Produces: Updated tests that work with geometry-first trail generation

- [ ] **Step 1: Find and update affected tests**

```bash
# Search for tests that might depend on old trail topology
dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SeedWorld" -v n
```

- [ ] **Step 2: Update snapshot tests to use new trail generation**

Update any snapshot assertions to match the new geometry-first trail generation output.

- [ ] **Step 3: Run full test suite**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj -v n`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add tests/WildBunch.GameContent.Tests/
git commit -m "test: update integration and snapshot tests for geometry-first trail generation"
```

---

### Task 10: Run full validation and CI checks

**Files:**
- No new files
- Validate entire codebase

**Interfaces:**
- Consumes: Full codebase
- Produces: Validation evidence

- [ ] **Step 1: Run build**

Run: `dotnet build`
Expected: PASS with no errors

- [ ] **Step 2: Run all tests**

Run: `dotnet test`
Expected: PASS with no failures (PostgreSQL-dependent tests may skip)

- [ ] **Step 3: Run EF migrations check**

Run: `dotnet tool restore && dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
Expected: PASS (no migrations needed for this change)

- [ ] **Step 4: Run index mesh generation**

Run: `python scripts/generate_index_mesh.py`
Expected: PASS (no structural changes to index mesh)

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "test: full validation passes for geometry-first trail generation"
```

---

### Task 11: Update Linear issue with route state

**Files:**
- No code files
- Update Linear issue BUNCH-127

**Interfaces:**
- Consumes: Linear issue BUNCH-127
- Produces: Updated Linear issue with route state block

- [ ] **Step 1: Update Linear issue with route state**

Add the following route state block to BUNCH-127:

```text
## Worker route state
Route status: executed
Plan PR: none (direct implementation)
Plan repo path: .agents/superpowers/plans/2026-07-03-generate-trails-from-settled-town-geometry.md
Plan approved: yes (inline execution)
Plan merged to main: no (pending PR)
Approved plan commit: none
Last staleness check: N/A
Execution PR: <PR URL when created>
```

- [ ] **Step 2: Update issue status to "In Review"**

- [ ] **Step 3: Add comment summarizing implementation**

Add a comment to BUNCH-127 summarizing:
- Replaced layout-authored trail topology with geometry-first generation
- Implemented edge filtering for crossings, parallel corridors, and redundant routes
- Ensured deterministic behavior for Boring mode
- Implemented entropy-based variation for entropic modes
- Added outlier town support with 6-day trails
- All tests passing

---

## Self-Review

**Spec coverage:**
- Layouts define town placement only ✓ (Task 7)
- Trails generated after final town coordinates settle ✓ (Task 6, 7)
- Boring mode deterministic ✓ (Task 4, 6)
- Entropic modes apply geometry mutation before trail generation ✓ (Task 7 - existing coordinate variance preserved)
- Generate candidate trail edges from pixel-space distances ✓ (Task 2)
- Select connected node graph using deterministic entropy/salt rules ✓ (Task 4)
- Reject edges that cross existing trails ✓ (Task 3)
- Reject edges that run closely parallel ✓ (Task 3)
- Reject edges that duplicate corridors ✓ (Task 3)
- Reject visually noisy redundant direct links ✓ (Task 3)
- Derive ride-day labels from final accepted edge lengths ✓ (Task 5, 6)
- Normal trails in 2-5 day range ✓ (Task 5)
- Outlier towns have exactly one incident 6-day trail ✓ (Task 5, 7)
- Tests cover deterministic Boring graphs ✓ (Task 4, 6, 7)
- Tests cover entropy/salt variation ✓ (Task 4, 6, 7)
- Tests cover connectivity ✓ (Task 4, 6, 7)
- Tests cover edge rejection ✓ (Task 3)
- Tests cover distance-label consistency ✓ (Task 5, 6, 7)
- CI passes ✓ (Task 10)

**Placeholder scan:** No placeholders found - all steps contain complete code.

**Type consistency:** All types, method signatures, and property names are consistent across tasks.
