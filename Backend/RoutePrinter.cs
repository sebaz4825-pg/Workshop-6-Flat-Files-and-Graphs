using System.Globalization;
using System.Text;

namespace Backend;

public sealed class RoutePrinter
{
    private readonly Graph<string> _city;
    private readonly IReadOnlyList<Package> _packages;
    private readonly string _start;
    private readonly RoutePlanner _planner;
    private readonly int _oneWayStreetCount;

    public RoutePrinter(
        Graph<string> city,
        IReadOnlyList<Package> packages,
        string start,
        RoutePlanner planner,
        int oneWayStreetCount = 0)
    {
        _city = city ?? throw new ArgumentNullException(nameof(city));
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _start = !string.IsNullOrWhiteSpace(start)
            ? start
            : throw new ArgumentException("Start location cannot be empty.", nameof(start));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _oneWayStreetCount = oneWayStreetCount;
    }

    public string Format(List<Stop> route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var sb = new StringBuilder();
        sb.AppendLine($"Posición inicial: {_start}");
        sb.AppendLine($"Paquetes: {_packages.Count} Ubicaciones: {_city.VertexCount}");

        if (_oneWayStreetCount > 0)
        {
            int totalStreets = _city.EdgeCount - _oneWayStreetCount;
            sb.AppendLine($"Calles: {totalStreets} ({_oneWayStreetCount} de un solo sentido)");
        }

        // Group consecutive stops sharing the same location.
        var groups = new List<List<Stop>>();
        int i = 0;
        while (i < route.Count)
        {
            int j = i + 1;
            while (j < route.Count && route[j].Location == route[i].Location)
            {
                j++;
            }

            var orderedGroup = route.GetRange(i, j - i)
                .OrderBy(s => s.Kind == StopKind.Delivery ? 0 : 1)
                .ThenBy(s => s.PackageId)
                .ToList();

            groups.Add(orderedGroup);
            i = j;
        }

        string currentLocation = _start;
        double totalMeters = 0.0;
        int stopNumber = 1;

        foreach (List<Stop> group in groups)
        {
            string destination = group[0].Location;
            double segmentMeters = _planner.DistanceBetween(currentLocation, destination);
            totalMeters += segmentMeters;

            string actionsText = FormatActions(group);
            List<string> path = _planner.PathBetween(currentLocation, destination);

            if (stopNumber == 1 && currentLocation == destination && Math.Abs(segmentMeters) < 1e-6)
            {
                sb.AppendLine($"{stopNumber}. En {destination}: {actionsText}.");
            }
            else
            {
                sb.AppendLine($"{stopNumber}. Ir de {currentLocation} a {destination} ({segmentMeters:0} m): {actionsText}.");
            }

            if (path.Count > 0)
            {
                sb.AppendLine($"Camino: {string.Join(" -> ", path)}");
            }
            else
            {
                sb.AppendLine($"Camino: {currentLocation} -> {destination}");
            }

            currentLocation = destination;
            stopNumber++;
        }

        sb.AppendLine("Todos los paquetes fueron recogidos y entregados.");

        double totalKilometers = totalMeters / 1000.0;
        sb.AppendLine($"Recorrido total: {totalKilometers.ToString("0.00", CultureInfo.InvariantCulture)} km");

        return sb.ToString().TrimEnd();
    }

    private static string FormatActions(List<Stop> group)
    {
        var deliveries = group.Where(s => s.Kind == StopKind.Delivery).Select(s => s.PackageId).ToList();
        var pickups = group.Where(s => s.Kind == StopKind.Pickup).Select(s => s.PackageId).ToList();

        var parts = new List<string>(2);

        if (deliveries.Count > 0)
        {
            parts.Add(deliveries.Count == 1
                ? $"entregar paquete {deliveries[0]}"
                : $"entregar paquetes {FormatIdList(deliveries)}");
        }

        if (pickups.Count > 0)
        {
            parts.Add(pickups.Count == 1
                ? $"recoger paquete {pickups[0]}"
                : $"recoger paquetes {FormatIdList(pickups)}");
        }

        return string.Join(" y ", parts);
    }

    private static string FormatIdList(List<int> ids)
    {
        if (ids.Count == 1)
        {
            return ids[0].ToString(CultureInfo.InvariantCulture);
        }

        if (ids.Count == 2)
        {
            return $"{ids[0]} y {ids[1]}";
        }

        string allExceptLast = string.Join(", ", ids.Take(ids.Count - 1));
        return $"{allExceptLast} y {ids[^1]}";
    }
}