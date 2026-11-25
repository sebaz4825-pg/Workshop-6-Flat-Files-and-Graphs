namespace Backend;

public sealed class RoutePlanner
{
    private readonly Graph<string> _city;
    private readonly IReadOnlyList<Package> _packages;
    private readonly string _start;
    private readonly Dictionary<string, Dictionary<string, double>> _distances = new();
    private readonly Dictionary<string, Dictionary<string, string?>> _previous = new();

    public RoutePlanner(Graph<string> city, IReadOnlyList<Package> packages, string start)
    {
        _city = city ?? throw new ArgumentNullException(nameof(city));
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _start = !string.IsNullOrWhiteSpace(start)
            ? start
            : throw new ArgumentException("Start location cannot be empty.", nameof(start));

        if (!_city.ContainsVertex(_start))
        {
            throw new ArgumentException($"Start location '{_start}' does not exist in the city map.", nameof(start));
        }

        PrecomputeShortestPaths();
    }

    private void PrecomputeShortestPaths()
    {
        var importantLocations = new HashSet<string> { _start };
        foreach (Package pkg in _packages)
        {
            importantLocations.Add(pkg.Pickup);
            importantLocations.Add(pkg.Delivery);
        }

        var dijkstra = new ShortestPaths<string>(_city);
        foreach (string location in importantLocations)
        {
            var (distMap, prevMap) = dijkstra.ComputeFrom(location);
            _distances[location] = distMap;
            _previous[location] = prevMap;
        }
    }

    public double DistanceBetween(string from, string to)
    {
        if (from == to)
        {
            return 0.0;
        }

        return _distances[from][to];
    }

    public List<string> PathBetween(string from, string to)
    {
        if (!_previous.TryGetValue(from, out Dictionary<string, string?>? prevMap))
        {
            var dijkstra = new ShortestPaths<string>(_city);
            (_, prevMap) = dijkstra.ComputeFrom(from);
            _previous[from] = prevMap;
        }

        return ShortestPaths<string>.ReconstructPath(prevMap, from, to);
    }

    public List<Stop> Plan()
    {
        if (_packages.Count == 0)
        {
            return new List<Stop>();
        }

        // 1. Baseline greedy nearest-neighbor route.
        List<Stop> bestRoute = BuildNearestNeighborRoute();
        bestRoute = LocalSearch(bestRoute);
        double bestCost = TotalDistance(bestRoute);

        // 2. Deterministic insertion order (sorted by distance from start).
        var orderedByStart = _packages
            .OrderBy(p => DistanceBetween(_start, p.Pickup) + DistanceBetween(p.Pickup, p.Delivery))
            .ThenBy(p => p.Id)
            .ToList();

        List<Stop> deterministicInsertion = LocalSearch(BuildInsertionRoute(orderedByStart));
        double deterministicCost = TotalDistance(deterministicInsertion);
        if (IsValid(deterministicInsertion) && deterministicCost < bestCost)
        {
            bestRoute = deterministicInsertion;
            bestCost = deterministicCost;
        }

        // 3. Multi-start randomized insertion + local search with fixed seed for reproducibility.
        var rng = new Random(2026);
        int restarts = _packages.Count <= 8 ? 30 : 80;

        for (int attempt = 0; attempt < restarts; attempt++)
        {
            var shuffled = _packages.ToList();
            Shuffle(shuffled, rng);

            List<Stop> candidate = BuildInsertionRoute(shuffled);
            candidate = LocalSearch(candidate);

            double candidateCost = TotalDistance(candidate);
            if (IsValid(candidate) && candidateCost < bestCost)
            {
                bestRoute = candidate;
                bestCost = candidateCost;
            }
        }

        return NormalizeSameLocationStops(bestRoute);
    }

    public double TotalDistance(List<Stop> route)
    {
        ArgumentNullException.ThrowIfNull(route);

        double total = 0.0;
        string current = _start;

        foreach (Stop stop in route)
        {
            total += DistanceBetween(current, stop.Location);
            current = stop.Location;
        }

        return total;
    }

    public bool IsValid(List<Stop> route)
    {
        if (route is null || route.Count != _packages.Count * 2)
        {
            return false;
        }

        var expectedPackages = _packages.ToDictionary(p => p.Id);
        var pickedUp = new HashSet<int>();
        var delivered = new HashSet<int>();

        foreach (Stop stop in route)
        {
            if (!expectedPackages.TryGetValue(stop.PackageId, out Package pkg))
            {
                return false;
            }

            if (stop.Kind == StopKind.Pickup)
            {
                if (stop.Location != pkg.Pickup || !pickedUp.Add(stop.PackageId))
                {
                    return false;
                }
            }
            else
            {
                if (stop.Location != pkg.Delivery || !pickedUp.Contains(stop.PackageId) || !delivered.Add(stop.PackageId))
                {
                    return false;
                }
            }
        }

        return pickedUp.Count == _packages.Count && delivered.Count == _packages.Count;
    }

    private List<Stop> BuildNearestNeighborRoute()
    {
        var route = new List<Stop>(_packages.Count * 2);
        var remainingPickups = new HashSet<int>(_packages.Select(p => p.Id));
        var activeDeliveries = new HashSet<int>();
        var packageMap = _packages.ToDictionary(p => p.Id);

        string currentLocation = _start;

        while (remainingPickups.Count > 0 || activeDeliveries.Count > 0)
        {
            Stop? bestStop = null;
            double bestDist = double.PositiveInfinity;

            // Prefer deliveries first on ties so packages get completed quickly.
            foreach (int id in activeDeliveries)
            {
                string loc = packageMap[id].Delivery;
                double dist = DistanceBetween(currentLocation, loc);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestStop = new Stop(loc, StopKind.Delivery, id);
                }
            }

            foreach (int id in remainingPickups)
            {
                string loc = packageMap[id].Pickup;
                double dist = DistanceBetween(currentLocation, loc);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestStop = new Stop(loc, StopKind.Pickup, id);
                }
            }

            Stop chosen = bestStop!.Value;
            route.Add(chosen);
            currentLocation = chosen.Location;

            if (chosen.Kind == StopKind.Pickup)
            {
                remainingPickups.Remove(chosen.PackageId);
                activeDeliveries.Add(chosen.PackageId);
            }
            else
            {
                activeDeliveries.Remove(chosen.PackageId);
            }
        }

        return route;
    }

    private List<Stop> BuildInsertionRoute(IReadOnlyList<Package> orderedPackages)
    {
        var route = new List<Stop>(orderedPackages.Count * 2);

        foreach (Package pkg in orderedPackages)
        {
            InsertPackageAtBestPosition(route, pkg);
        }

        return route;
    }

    private void InsertPackageAtBestPosition(List<Stop> route, Package pkg)
    {
        var pickupStop = new Stop(pkg.Pickup, StopKind.Pickup, pkg.Id);
        var deliveryStop = new Stop(pkg.Delivery, StopKind.Delivery, pkg.Id);

        int bestPickupIndex = 0;
        int bestDeliveryIndex = 1;
        double bestCost = double.PositiveInfinity;

        int n = route.Count;
        for (int pickupIdx = 0; pickupIdx <= n; pickupIdx++)
        {
            route.Insert(pickupIdx, pickupStop);

            for (int deliveryIdx = pickupIdx + 1; deliveryIdx <= n + 1; deliveryIdx++)
            {
                route.Insert(deliveryIdx, deliveryStop);

                double cost = TotalDistance(route);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestPickupIndex = pickupIdx;
                    bestDeliveryIndex = deliveryIdx;
                }

                route.RemoveAt(deliveryIdx);
            }

            route.RemoveAt(pickupIdx);
        }

        route.Insert(bestPickupIndex, pickupStop);
        route.Insert(bestDeliveryIndex, deliveryStop);
    }

    private List<Stop> LocalSearch(List<Stop> initialRoute)
    {
        var route = new List<Stop>(initialRoute);
        double currentBestCost = TotalDistance(route);
        bool improved = true;

        while (improved)
        {
            improved = false;

            // Phase A: Relocate a full package (Pickup + Delivery pair).
            foreach (Package pkg in _packages)
            {
                int pickupIdx = route.FindIndex(s => s.PackageId == pkg.Id && s.Kind == StopKind.Pickup);
                int deliveryIdx = route.FindIndex(s => s.PackageId == pkg.Id && s.Kind == StopKind.Delivery);

                if (pickupIdx < 0 || deliveryIdx < 0 || pickupIdx == deliveryIdx)
                {
                    continue;
                }

                Stop pickupStop = route[pickupIdx];
                Stop deliveryStop = route[deliveryIdx];

                int firstIdx = Math.Min(pickupIdx, deliveryIdx);
                int secondIdx = Math.Max(pickupIdx, deliveryIdx);

                route.RemoveAt(secondIdx);
                route.RemoveAt(firstIdx);

                int bestP = pickupIdx;
                int bestD = deliveryIdx;
                double bestPairCost = currentBestCost;

                int n = route.Count;
                for (int pIdx = 0; pIdx <= n; pIdx++)
                {
                    route.Insert(pIdx, pickupStop);

                    for (int dIdx = pIdx + 1; dIdx <= route.Count; dIdx++)
                    {
                        route.Insert(dIdx, deliveryStop);

                        double candidateCost = TotalDistance(route);
                        if (candidateCost + 1e-6 < bestPairCost)
                        {
                            bestPairCost = candidateCost;
                            bestP = pIdx;
                            bestD = dIdx;
                        }

                        route.RemoveAt(dIdx);
                    }

                    route.RemoveAt(pIdx);
                }

                // Restore best valid pair positions safely.
                bestP = Math.Clamp(bestP, 0, route.Count);
                route.Insert(bestP, pickupStop);
                bestD = Math.Clamp(bestD, bestP + 1, route.Count);
                route.Insert(bestD, deliveryStop);

                if (bestPairCost + 1e-6 < currentBestCost)
                {
                    currentBestCost = bestPairCost;
                    improved = true;
                }
            }

            // Phase B: Relocate a single stop while respecting Pickup < Delivery.
            for (int i = 0; i < route.Count; i++)
            {
                Stop movingStop = route[i];
                int minAllowed = 0;
                int maxAllowed = route.Count - 1;

                if (movingStop.Kind == StopKind.Pickup)
                {
                    int deliveryPos = route.FindIndex(s => s.PackageId == movingStop.PackageId && s.Kind == StopKind.Delivery);
                    if (deliveryPos <= i)
                    {
                        continue;
                    }
                    maxAllowed = deliveryPos - 1;
                }
                else
                {
                    int pickupPos = route.FindIndex(s => s.PackageId == movingStop.PackageId && s.Kind == StopKind.Pickup);
                    if (pickupPos < 0 || pickupPos >= i)
                    {
                        continue;
                    }
                    minAllowed = pickupPos + 1;
                }

                if (minAllowed > maxAllowed)
                {
                    continue;
                }

                route.RemoveAt(i);

                // After removing element at index i, indices > i shift left by 1.
                int remainingMaxIndex = route.Count;
                int safeMin = Math.Clamp(minAllowed, 0, remainingMaxIndex);
                int safeMax = Math.Clamp(maxAllowed, safeMin, remainingMaxIndex);

                int bestInsertPos = Math.Clamp(i, safeMin, safeMax);
                double bestSingleCost = currentBestCost;

                for (int pos = safeMin; pos <= safeMax; pos++)
                {
                    route.Insert(pos, movingStop);
                    double candidateCost = TotalDistance(route);
                    if (candidateCost + 1e-6 < bestSingleCost)
                    {
                        bestSingleCost = candidateCost;
                        bestInsertPos = pos;
                    }
                    route.RemoveAt(pos);
                }

                bestInsertPos = Math.Clamp(bestInsertPos, 0, route.Count);
                route.Insert(bestInsertPos, movingStop);

                if (bestSingleCost + 1e-6 < currentBestCost)
                {
                    currentBestCost = bestSingleCost;
                    improved = true;
                }
            }
        }

        return route;
    }

    private static List<Stop> NormalizeSameLocationStops(List<Stop> route)
    {
        // Group consecutive stops at the same location with Deliveries before Pickups, sorted by PackageId.
        var normalized = new List<Stop>(route.Count);
        int i = 0;

        while (i < route.Count)
        {
            int j = i + 1;
            while (j < route.Count && route[j].Location == route[i].Location)
            {
                j++;
            }

            var block = route.GetRange(i, j - i)
                .OrderBy(s => s.Kind == StopKind.Delivery ? 0 : 1)
                .ThenBy(s => s.PackageId);

            normalized.AddRange(block);
            i = j;
        }

        return normalized;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}