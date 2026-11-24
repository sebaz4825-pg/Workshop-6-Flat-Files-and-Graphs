namespace Backend;

public sealed class ShortestPaths<T> where T : notnull
{
    private readonly Graph<T> _graph;

    public ShortestPaths(Graph<T> graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    public (Dictionary<T, double> Distances, Dictionary<T, T?> Previous) ComputeFrom(T source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!_graph.ContainsVertex(source))
        {
            throw new KeyNotFoundException($"Source location '{source}' does not exist in the map.");
        }

        var distances = new Dictionary<T, double>();
        var previous = new Dictionary<T, T?>();

        foreach (T vertex in _graph.Vertices)
        {
            distances[vertex] = double.PositiveInfinity;
            previous[vertex] = default;
        }

        distances[source] = 0.0;

        var queue = new PriorityQueue<T, double>();
        queue.Enqueue(source, 0.0);

        while (queue.TryDequeue(out T? current, out double currentDistance))
        {
            if (current is null)
            {
                continue;
            }

            if (currentDistance > distances[current])
            {
                continue;
            }

            foreach (Edge<T> edge in _graph.GetNeighbors(current))
            {
                if (edge.Weight < 0)
                {
                    throw new InvalidOperationException("Dijkstra does not support negative edge weights.");
                }

                double candidateDistance = distances[current] + edge.Weight;
                if (candidateDistance < distances[edge.To])
                {
                    distances[edge.To] = candidateDistance;
                    previous[edge.To] = current;
                    queue.Enqueue(edge.To, candidateDistance);
                }
            }
        }

        return (distances, previous);
    }

    public static List<T> ReconstructPath(Dictionary<T, T?> previous, T start, T target)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(target);

        var path = new List<T>();

        if (EqualityComparer<T>.Default.Equals(start, target))
        {
            path.Add(start);
            return path;
        }

        T? current = target;
        while (current is not null && !EqualityComparer<T>.Default.Equals(current, start))
        {
            path.Add(current);
            if (!previous.TryGetValue(current, out current))
            {
                return new List<T>();
            }
        }

        if (current is null)
        {
            return new List<T>();
        }

        path.Add(start);
        path.Reverse();
        return path;
    }
}