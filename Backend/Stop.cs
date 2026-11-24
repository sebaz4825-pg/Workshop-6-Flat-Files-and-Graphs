namespace Backend;

public enum StopKind
{
    Pickup,
    Delivery
}

public readonly record struct Stop(string Location, StopKind Kind, int PackageId);