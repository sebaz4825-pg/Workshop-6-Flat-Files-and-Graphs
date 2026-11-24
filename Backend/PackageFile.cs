namespace Backend;

public static class PackageFile
{
    public static List<Package> Load(string path, Graph<string> city)
    {
        return Load(path, city, startLocation: null);
    }

    public static List<Package> Load(string path, Graph<string> city, string? startLocation)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Packages file path cannot be empty.", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(city);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Packages file not found: {path}", path);
        }

        if (startLocation is not null && !city.ContainsVertex(startLocation))
        {
            throw new ArgumentException($"Start location '{startLocation}' does not exist in the city map.");
        }

        var pickupsById = new Dictionary<int, string>();
        var deliveriesById = new Dictionary<int, string>();
        var taskLocations = new HashSet<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string trimmed = lines[i].Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            string[] parts = trimmed.Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException(
                    $"Line {lineNumber}: Expected 3 comma-separated fields (location, action, package), got {parts.Length}.");
            }

            string location = parts[0].Trim();
            string actionRaw = parts[1].Trim();
            string packageIdRaw = parts[2].Trim();

            if (string.IsNullOrEmpty(location) || !city.ContainsVertex(location))
            {
                throw new FormatException(
                    $"Line {lineNumber}: Location '{location}' does not exist in the city map.");
            }

            StopKind kind;
            if (string.Equals(actionRaw, "Pickup", StringComparison.OrdinalIgnoreCase))
            {
                kind = StopKind.Pickup;
            }
            else if (string.Equals(actionRaw, "Delivery", StringComparison.OrdinalIgnoreCase))
            {
                kind = StopKind.Delivery;
            }
            else
            {
                throw new FormatException(
                    $"Line {lineNumber}: Invalid action '{actionRaw}'. Expected 'Pickup' or 'Delivery'.");
            }

            if (!int.TryParse(packageIdRaw, out int packageId) || packageId <= 0)
            {
                throw new FormatException(
                    $"Line {lineNumber}: Package number must be an integer greater than 0, got '{packageIdRaw}'.");
            }

            if (kind == StopKind.Pickup)
            {
                if (!pickupsById.TryAdd(packageId, location))
                {
                    throw new FormatException(
                        $"Line {lineNumber}: Duplicate Pickup for package {packageId}.");
                }
            }
            else
            {
                if (!deliveriesById.TryAdd(packageId, location))
                {
                    throw new FormatException(
                        $"Line {lineNumber}: Duplicate Delivery for package {packageId}.");
                }
            }

            taskLocations.Add(location);
        }

        var allPackageIds = new SortedSet<int>(pickupsById.Keys);
        allPackageIds.UnionWith(deliveriesById.Keys);

        if (allPackageIds.Count == 0)
        {
            throw new InvalidDataException($"Packages file '{path}' contains no package tasks.");
        }

        var packages = new List<Package>(allPackageIds.Count);

        foreach (int id in allPackageIds)
        {
            bool hasPickup = pickupsById.TryGetValue(id, out string? pickupLocation);
            bool hasDelivery = deliveriesById.TryGetValue(id, out string? deliveryLocation);

            if (!hasPickup && !hasDelivery)
            {
                throw new InvalidDataException($"Package {id} is missing both Pickup and Delivery.");
            }

            if (!hasPickup)
            {
                throw new InvalidDataException($"Package {id} is missing its Pickup task.");
            }

            if (!hasDelivery)
            {
                throw new InvalidDataException($"Package {id} is missing its Delivery task.");
            }

            packages.Add(new Package(id, pickupLocation!, deliveryLocation!));
        }

        if (startLocation is not null)
        {
            ValidateReachability(city, startLocation, taskLocations);
        }

        return packages;
    }

    private static void ValidateReachability(Graph<string> city, string startLocation, HashSet<string> taskLocations)
    {
        var shortestPaths = new ShortestPaths<string>(city);

        var (fromStartDistances, _) = shortestPaths.ComputeFrom(startLocation);
        foreach (string loc in taskLocations)
        {
            if (double.IsInfinity(fromStartDistances[loc]))
            {
                throw new InvalidOperationException(
                    $"Location '{loc}' is not reachable from start location '{startLocation}'.");
            }
        }

        var distancesByTaskLoc = new Dictionary<string, Dictionary<string, double>>();
        foreach (string origin in taskLocations)
        {
            var (distMap, _) = shortestPaths.ComputeFrom(origin);
            distancesByTaskLoc[origin] = distMap;
        }

        foreach (string origin in taskLocations)
        {
            foreach (string destination in taskLocations)
            {
                if (origin == destination)
                {
                    continue;
                }

                if (double.IsInfinity(distancesByTaskLoc[origin][destination]))
                {
                    throw new InvalidOperationException(
                        $"Location '{destination}' cannot be reached from task location '{origin}'.");
                }
            }
        }
    }
}