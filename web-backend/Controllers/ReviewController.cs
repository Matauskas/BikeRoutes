using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using web_backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using web_backend.Entities;
using web_backend.Repositories;
using System.Text.Json.Serialization;
namespace web_backend.Controllers;

public class CreateReviewPayload
{

    [Required]
    public int RouteId { get; set; }
    [Required]
    public int Rating { get; set; }

    [Required]
    public string Description { get; set; }

}

[Authorize]
[ApiController]
[Route("/Reviews")]

public class ReviewsController : ControllerBase
{
    private readonly ReviewsRepository reviews;

    public ReviewsController(ReviewsRepository reviews)
    {
        this.reviews = reviews;
    }

    [Authorize]
    [HttpPost("createReview")]
    public async Task<ActionResult> Create([FromBody] CreateReviewPayload payload)
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return Unauthorized();

        var review = new Review
        {
            UserId = userId.Value,
            RouteId = payload.RouteId,
            Rating = payload.Rating,
            Description = payload.Description
        };

        var routeId = reviews.CreateReview(review);

        if (routeId == false)
        {
            return ErrorReason.BadRequest(new ErrorReason("cant_save"));
        }

        return Ok("Zjbs");
    }

    [HttpGet("{routeId}")]
    public ActionResult<List<Review>> ListReviews(int routeId)
    {
        return reviews.ListReviews(routeId);
    }




}