using System.Globalization;

namespace Backend;

public static class CityFile
{
    public static Graph<string> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("City file path cannot be empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"City file not found: {path}", path);
        }

        var graph = new Graph<string>(directed: true);
        var seenStreets = new HashSet<(string A, string B)>();

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
            if (parts.Length != 4)
            {
                throw new FormatException(
                    $"Line {lineNumber}: Expected 4 comma-separated fields (from, to, distance_in_meters, direction), got {parts.Length}.");
            }

            string from = parts[0].Trim();
            string to = parts[1].Trim();
            string distanceRaw = parts[2].Trim();
            string directionRaw = parts[3].Trim();

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                throw new FormatException($"Line {lineNumber}: 'from' and 'to' locations cannot be empty.");
            }

            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                throw new FormatException($"Line {lineNumber}: Self-loop street is not allowed ('{from}' -> '{to}').");
            }

            if (!double.TryParse(distanceRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out double distance) ||
                double.IsNaN(distance) ||
                double.IsInfinity(distance) ||
                distance <= 0)
            {
                throw new FormatException(
                    $"Line {lineNumber}: Distance must be a number greater than 0, got '{distanceRaw}'.");
            }

            bool isTwoWay;
            if (string.Equals(directionRaw, "Twoway", StringComparison.OrdinalIgnoreCase))
            {
                isTwoWay = true;
            }
            else if (string.Equals(directionRaw, "Oneway", StringComparison.OrdinalIgnoreCase))
            {
                isTwoWay = false;
            }
            else
            {
                throw new FormatException(
                    $"Line {lineNumber}: Invalid direction '{directionRaw}'. Expected 'Twoway' or 'Oneway'.");
            }

            // Normalize pair so (A, B) and (B, A) are detected as the same street.
            (string first, string second) canonicalPair = string.CompareOrdinal(from, to) <= 0
                ? (from, to)
                : (to, from);

            if (!seenStreets.Add(canonicalPair))
            {
                throw new FormatException(
                    $"Line {lineNumber}: Duplicate street between '{from}' and '{to}'.");
            }

            graph.AddEdge(from, to, distance);
            if (isTwoWay)
            {
                graph.AddEdge(to, from, distance);
            }
        }

        if (graph.VertexCount == 0)
        {
            throw new InvalidDataException($"City file '{path}' contains no valid street data.");
        }

        return graph;
    }
}