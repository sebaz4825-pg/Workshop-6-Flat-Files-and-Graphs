namespace Backend;

public sealed class Graph<T> where T : notnull
{
    private readonly Dictionary<T, List<Edge<T>>> _adjacency = new();

    public bool IsDirected { get; }

    public int VertexCount => _adjacency.Count;

    public int EdgeCount => _adjacency.Values.Sum(list => list.Count);

    public IEnumerable<T> Vertices => _adjacency.Keys;

    public Graph(bool directed = true)
    {
        IsDirected = directed;
    }

    public bool AddVertex(T vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);

        if (_adjacency.ContainsKey(vertex))
        {
            return false;
        }

        _adjacency[vertex] = new List<Edge<T>>();
        return true;
    }

    public bool ContainsVertex(T vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        return _adjacency.ContainsKey(vertex);
    }

    public void AddEdge(T from, T to, double weight)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        AddVertex(from);
        AddVertex(to);

        _adjacency[from].Add(new Edge<T>(to, weight));

        if (!IsDirected)
        {
            _adjacency[to].Add(new Edge<T>(from, weight));
        }
    }

    public IReadOnlyList<Edge<T>> GetNeighbors(T vertex)
    {
        ArgumentNullException.ThrowIfNull(vertex);

        return _adjacency.TryGetValue(vertex, out List<Edge<T>>? edges)
            ? edges
            : Array.Empty<Edge<T>>();
    }
}