using web_backend.Entities;

namespace web_backend.Repositories;

public class ReviewsRepository
{
    private readonly BaseDbContext db;
    public ReviewsRepository(BaseDbContext context)
    {
        db = context ?? throw new ArgumentNullException(nameof(context));
    }
    public bool CreateReview(Review review)
    {
        db.Reviews.Add(new Review
        {
            UserId = review.UserId,
            RouteId = review.RouteId,
            Rating = review.Rating,
            Description = review.Description
        });

        return db.SaveChanges() > 0;
    }
    public List<Review> ListReviews(int routeId)
    {
        return db.Reviews
            .Where(req => req.RouteId == routeId)
            .ToList();
    }
}