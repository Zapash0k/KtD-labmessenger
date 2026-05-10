using MessengerApi.Models;
using MessengerApi.Storage;
using Microsoft.EntityFrameworkCore;

namespace MessengerApi.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.");

        var user = new User { Name = request.Name.Trim() };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return ToResponse(user);
    }

    public async Task<UserResponse> GetUserAsync(string id)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User '{id}' not found.");
        return ToResponse(user);
    }

    public async Task<List<UserResponse>> GetAllUsersAsync()
    {
        var users = await _db.Users.OrderBy(u => u.Name).ToListAsync();
        return users.Select(ToResponse).ToList();
    }

    public async Task<User> GetEntityAsync(string id) =>
        await _db.Users.FindAsync(id)
        ?? throw new KeyNotFoundException($"User '{id}' not found.");

    private static UserResponse ToResponse(User u) =>
        new(u.Id, u.Name, u.CreatedAt);
}