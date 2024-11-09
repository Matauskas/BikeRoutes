using web_backend.Entities;

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RouteId { get; set; }
    public int Rating { get; set; }
    public string Description { get; set; }
}