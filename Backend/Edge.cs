namespace Backend;

public sealed record Edge<T>(T To, double Weight) where T : notnull;