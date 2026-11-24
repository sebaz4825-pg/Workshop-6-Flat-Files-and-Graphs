using Backend;

string cityPath;
string packagesPath;
string startLocation;

if (args.Length >= 3)
{
    cityPath = args[0];
    packagesPath = args[1];
    startLocation = args[2].Trim();
}
else
{
    Console.Write("Ruta del archivo de ciudad (ej. city.txt): ");
    cityPath = (Console.ReadLine() ?? string.Empty).Trim();

    Console.Write("Ruta del archivo de paquetes (ej. packages.txt): ");
    packagesPath = (Console.ReadLine() ?? string.Empty).Trim();

    Console.Write("Posición inicial (ej. D): ");
    startLocation = (Console.ReadLine() ?? string.Empty).Trim();
}

try
{
    Graph<string> city = CityFile.Load(cityPath);
    int oneWayCount = CountOneWayStreets(cityPath);

    if (string.IsNullOrWhiteSpace(startLocation) || !city.ContainsVertex(startLocation))
    {
        Console.Error.WriteLine($"Error: La posición inicial '{startLocation}' no existe en el mapa.");
        Environment.ExitCode = 1;
        return;
    }

    List<Package> packages = PackageFile.Load(packagesPath, city, startLocation);

    var planner = new RoutePlanner(city, packages, startLocation);
    List<Stop> route = planner.Plan();

    if (!planner.IsValid(route))
    {
        Console.Error.WriteLine("Error: La ruta generada no cumple con las reglas de recogida y entrega.");
        Environment.ExitCode = 1;
        return;
    }

    var printer = new RoutePrinter(city, packages, startLocation, planner, oneWayCount);
    Console.WriteLine(printer.Format(route));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error de validación o ejecución: {ex.Message}");
    Environment.ExitCode = 1;
}

static int CountOneWayStreets(string cityFilePath)
{
    int oneWay = 0;
    foreach (string rawLine in File.ReadAllLines(cityFilePath))
    {
        string line = rawLine.Trim();
        if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
        {
            continue;
        }

        string[] parts = line.Split(',');
        if (parts.Length == 4 && string.Equals(parts[3].Trim(), "Oneway", StringComparison.OrdinalIgnoreCase))
        {
            oneWay++;
        }
    }

    return oneWay;
}