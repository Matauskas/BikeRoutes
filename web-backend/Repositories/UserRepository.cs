using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Threading.Tasks;
using web_backend;

public class UserRepository
{
    private readonly BaseDbContext _context;

    public UserRepository(BaseDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public User? GetUserByUsername(string username)
    {
       return _context.Users.FirstOrDefault(u => u.Username == username);
    }
    public async Task<User> GetUserByEmailAsync(string email)
    {

        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public List<User> List()
    {
        return _context.Users.ToList();
    }

    public async Task<User?> GetUserById(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public bool RegisterUser(User user)
    {
        user.Password = HashPassword(user.Password);

        _context.Users.Add(user);
        return _context.SaveChanges() > 0;
    }

    public async Task<bool> ChangeUserPsw(User user, string newPassword)
    {
        user.Password = HashPassword(newPassword);

        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<bool> ResetPassword(User user, string password)
    {
        user.Password = HashPassword(password);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ChangePhotoUrl(User user, string photoUrl)
    {
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<string?> GetUserProfileImageUrl(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.PhotoUrl;
    }

    public async Task<bool> DeleteById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateUserAsync(string username, string latitude, string longitude)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            // User not found
            return false;
        }

        user.Latitude = latitude;
        user.Longitude = longitude;

        _context.Users.Update(user);
        return await _context.SaveChangesAsync() > 0;
    }
    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
