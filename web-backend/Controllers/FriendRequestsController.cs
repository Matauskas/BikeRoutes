using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using web_backend.Repositories;

namespace web_backend.Controllers;

public class FriendRequstPayload
{
    [Required]
    public int Id { get; set; }
}

[Authorize]
[ApiController]
[Route("/friend-requests")]
public class FriendRequestsController : ControllerBase
{
    private readonly FriendsRepository friends;
    private readonly UserRepository users;

    public FriendRequestsController(FriendsRepository friends, UserRepository users)
    {
        this.friends = friends;
        this.users = users;
    }

    [HttpPost]
    public async Task<ActionResult> SendFriendRequest([FromBody] FriendRequstPayload payload)
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return Unauthorized();


        var fromUserId = userId.Value;
        var toUserId = payload.Id;
        if (fromUserId == toUserId)
        {
            return ErrorReason.BadRequest(new ErrorReason("cant_send_to_yourself"));
        }

        if (friends.AreFriends(fromUserId, toUserId))
        {
            return ErrorReason.BadRequest(new ErrorReason("already_friends"));
        }

        if (friends.HasRequest(fromUserId, toUserId))
        {
            return ErrorReason.BadRequest(new ErrorReason("already_sent"));
        }

        if (friends.HasRequest(toUserId, fromUserId)) {
            bool success = friends.RemoveRequest(toUserId, fromUserId);
            if (!success)
            {
                return ErrorReason.BadRequest(new ErrorReason("unknown")); // TODO: better error message
            }

            success = friends.CreateFriends(fromUserId, toUserId);
            if (!success)
            {
                return ErrorReason.BadRequest(new ErrorReason("unknown")); // TODO: better error message
            }

        } else {
            bool success = friends.CreateRequest(fromUserId, toUserId);
            if (!success)
            {
                return ErrorReason.BadRequest(new ErrorReason("unknown")); // TODO: better error message
            }
        }

        return Ok();
    }

    [HttpGet("incoming")]
    public ActionResult<List<int>> ListIncomingRequests()
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return Unauthorized();

        return friends.ListIncomingRequests(userId.Value);
    }

    // TODO: Add revoking of incoming friend requests. Maybe also blocking of anoying people who keep sending requests?

    [HttpGet("outgoing")]
    public ActionResult<List<int>> ListOutgoingRequests()
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return Unauthorized();

        return friends.ListOutgoingRequests(userId.Value);
    }

    // TODO: Add canceling of outgoing friend requests.
}

