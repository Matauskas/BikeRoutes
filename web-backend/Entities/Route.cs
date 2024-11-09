using Newtonsoft.Json;
using web_backend.Entities;

public class DbRoute
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int OwnerId { get; set; }
    public int PointListId { get; set; }
    public double Distance { get; set; }
    public double Time { get; set; }
}

public class Route
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int OwnerId { get; set; }
    public List<Point> Points { get; set; }
    public double Distance { get; set; }
    public double Time { get; set; }
}

public class UpdateRouteOptions
{
    public string? Title { get; set; }

    public List<Point>? points { get; set; }

    public double? Distance { get; set; }

    public double? Time { get; set; }
}