using System.Data.Entity;
using web_backend.Entities;

namespace web_backend.Repositories;

public class FriendsRepository
{
    private readonly BaseDbContext db;

    public FriendsRepository(BaseDbContext context)
    {
        db = context ?? throw new ArgumentNullException(nameof(context));
    }

    public List<int> ListOutgoingRequests(int fromUser)
    {
        return db.FriendRequests
            .Where(req => req.FromUser == fromUser)
            .Select(req => req.ToUser)
            .ToList();
    }

    public List<int> ListIncomingRequests(int toUser)
    {
        return db.FriendRequests
            .Where(req => req.ToUser == toUser)
            .Select(req => req.FromUser)
            .ToList();
    }

    public bool CreateRequest(int fromUser, int toUser)
    {
        Console.WriteLine($"{fromUser} {toUser}");
        db.FriendRequests.Add(new FriendRequest
        {
            FromUser = fromUser,
            ToUser = toUser
        });

        return db.SaveChanges() > 0;
    }

    public bool RemoveRequest(int fromUSer, int toUser)
    {
        var req = db.FriendRequests.Where(req => req.FromUser == fromUSer && req.ToUser == toUser).FirstOrDefault();
        if (req == null) return false;

        db.FriendRequests.Remove(req);
        return db.SaveChanges() > 0;
    }

    public bool HasRequest(int fromUser, int toUser)
    {
        return db.FriendRequests.Any(req => req.FromUser == fromUser && req.ToUser == toUser);
    }

    public bool AreFriends(int userA, int userB)
    {
        return db.Friendships.Any(req => (req.UserA == userA && req.UserB == userB) ||
                                         (req.UserA == userB && req.UserB == userA));
    }

    public List<int> ListFriends(int user)
    {
        var friends = new List<int>();

        friends.AddRange(db.Friendships.Where(friendship => friendship.UserA == user).Select(friendship => friendship.UserB));
        friends.AddRange(db.Friendships.Where(friendship => friendship.UserB == user).Select(friendship => friendship.UserA));

        return friends;
    }

    public bool RemoveFriends(int userA, int userB)
    {
        var friendship = db.Friendships
            .Where(req => (req.UserA == userA && req.UserB == userB) || (req.UserA == userB && req.UserB == userA))
            .FirstOrDefault();
        if (friendship == null) return false;

        db.Friendships.Remove(friendship);
        return db.SaveChanges() > 0;
    }

    public bool CreateFriends(int userA, int userB)
    {
        db.Friendships.Add(new Friendship
        {
            UserA = userA,
            UserB = userB
        });

        return db.SaveChanges() > 0;
    }
}
