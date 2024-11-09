using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace web_backend.Entities;

public class Point
{
    [Required]
    [JsonPropertyName("longtitude")]
    public double Longtitude { get; set; }

    [Required]
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
}

public class DbPoint
{
    public int ListId { get; set; }
    public int PointIndex { get; set; }
    public double Longtitude { get; set; }
    public double Latitude { get; set; }
}

public class DbPointList
{
    public int Id { get; set; }
}