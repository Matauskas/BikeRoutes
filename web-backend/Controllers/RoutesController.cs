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

public class CreateRoutePayload
{
    [Required]
    public string Title { get; set; }

    public int? OwnerId { get; set; }

    [Required]
    public List<Point> points { get; set; }

    [Required]
    public float Distance { get; set; }
    [Required]
    public float Time { get; set; }
}

[ApiController]
[Route("/Routes")]
public class RoutesController : ControllerBase
{
    private readonly RoutesRepository routes;
    private readonly PointsRepository points;

    public RoutesController(RoutesRepository routeRepository, PointsRepository points)
    {
        routes = routeRepository;
        this.points = points;
    }

    [Authorize]
    [HttpPost("save")]
    public async Task<ActionResult<Route>> Save([FromBody] CreateRoutePayload payload)
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return Unauthorized();

        int ownerId = payload.OwnerId ?? userId.Value;

        var route = new Route
        {
            Title = payload.Title,
            OwnerId = ownerId,
            Points = payload.points,
            Distance = payload.Distance,
            Time = payload.Time
        };

        var routeId = routes.SaveRoute(route);
        if (routeId == null)
        {
            return ErrorReason.BadRequest(new ErrorReason("cant_save"));
        }

        route.Id = routeId.Value;
        return Ok(route);
    }


    [Authorize]
    [HttpPut("{id}")]
    public ActionResult Update(int id, [FromBody] UpdateRouteOptions payload)
    {
        bool success = routes.Update(id, payload);
        if (!success)
        {
            return ErrorReason.BadRequest(new ErrorReason("smth"));
        }

        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<List<Route>>> ListRoutes()
    {
        return await routes.List();
    }
}