using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_backend.Repositories;

namespace web_backend.Controllers;

[Authorize]
[ApiController]
[Route("/friends")]
public class FriendsController : ControllerBase
{
    private readonly FriendsRepository friends;

    public FriendsController(FriendsRepository friends)
    {
        this.friends = friends;
    }

    [HttpGet]
    public ActionResult<List<int>> ListFriends()
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return Unauthorized();

        return friends.ListFriends(userId.Value);
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteFriend(int id)
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return Unauthorized();

        bool success = friends.RemoveFriends(userId.Value, id);
        if (!success)
        {
            return ErrorReason.BadRequest(new ErrorReason("friend_not_found")); // TODO: better error message
        }

        return Ok();
    }
}
