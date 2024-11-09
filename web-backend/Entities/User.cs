using System.Text.Json.Serialization;

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string LastName { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonIgnore]
    public string Password { get; set; }

    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; set; }

    [JsonPropertyName("totalDistance")]
    public float TotalDistance { get; set; } = 0;
    [JsonPropertyName("totalTime")]
    public float TotalTime { get; set; } = 0;
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; } = 0;
    [JsonPropertyName("Latitude")]
    public string? Latitude { get; set; }

    [JsonPropertyName("Longitude")]
    public string? Longitude { get; set; }
}