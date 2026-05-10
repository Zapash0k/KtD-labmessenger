using MessengerApi.Models;
using MessengerApi.Storage;
using Microsoft.EntityFrameworkCore;

namespace MessengerApi.Services;

public class ConversationService
{
    private readonly AppDbContext _db;

    public ConversationService(AppDbContext db) => _db = db;

    ///Creates a 1-on-1 direct conversation between two users.
    public async Task<ConversationResponse> CreateDirectAsync(CreateDirectConversationRequest request)
    {
        if (request.UserIdA == request.UserIdB)
            throw new ArgumentException("Cannot create a direct conversation with yourself.");

        await EnsureUsersExistAsync(request.UserIdA, request.UserIdB);

        // Prevent duplicate direct conversations
        var existing = await FindDirectConversationAsync(request.UserIdA, request.UserIdB);
        if (existing is not null)
            return await ToResponseAsync(existing);

        var conversation = new Conversation { Type = ConversationType.Direct };
        conversation.Members.Add(new ConversationMember { UserId = request.UserIdA });
        conversation.Members.Add(new ConversationMember { UserId = request.UserIdB });

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();
        return await ToResponseAsync(conversation);
    }

    ///Creates a group conversation with 2+ members (Variant 4).
    public async Task<ConversationResponse> CreateGroupAsync(CreateGroupConversationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Group name cannot be empty.");

        if (request.MemberIds == null || request.MemberIds.Count < 2)
            throw new ArgumentException("A group must have at least 2 members.");

        var distinctIds = request.MemberIds.Distinct().ToList();
        foreach (var id in distinctIds)
            await EnsureUsersExistAsync(id);

        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            Name = request.Name.Trim()
        };

        foreach (var userId in distinctIds)
            conversation.Members.Add(new ConversationMember { UserId = userId });

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();
        return await ToResponseAsync(conversation);
    }

    public async Task<ConversationResponse> GetConversationAsync(string id)
    {
        var conversation = await LoadConversationAsync(id)
            ?? throw new KeyNotFoundException($"Conversation '{id}' not found.");
        return await ToResponseAsync(conversation);
    }

    public async Task<List<ConversationResponse>> GetUserConversationsAsync(string userId)
    {
        await EnsureUsersExistAsync(userId);

        var conversations = await _db.Conversations
            .Include(c => c.Members)
            .ThenInclude(m => m.User)
            .Where(c => c.Members.Any(m => m.UserId == userId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var responses = new List<ConversationResponse>();
        foreach (var c in conversations)
            responses.Add(await ToResponseAsync(c));
        return responses;
    }

    public async Task<Conversation> GetEntityAsync(string id) =>
        await LoadConversationAsync(id)
        ?? throw new KeyNotFoundException($"Conversation '{id}' not found.");

    //Helpers

    private async Task<Conversation?> LoadConversationAsync(string id) =>
        await _db.Conversations
            .Include(c => c.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == id);

    private async Task<Conversation?> FindDirectConversationAsync(string userA, string userB) =>
        await _db.Conversations
            .Include(c => c.Members)
            .ThenInclude(m => m.User)
            .Where(c =>
                c.Type == ConversationType.Direct &&
                c.Members.Any(m => m.UserId == userA) &&
                c.Members.Any(m => m.UserId == userB))
            .FirstOrDefaultAsync();

    private async Task EnsureUsersExistAsync(params string[] ids)
    {
        foreach (var id in ids)
        {
            if (!await _db.Users.AnyAsync(u => u.Id == id))
                throw new KeyNotFoundException($"User '{id}' not found.");
        }
    }

    private static Task<ConversationResponse> ToResponseAsync(Conversation c) =>
        Task.FromResult(new ConversationResponse(
            c.Id,
            c.Type.ToString(),
            c.Name,
            c.Members.Select(m => new UserResponse(m.User.Id, m.User.Name, m.User.CreatedAt)).ToList(),
            c.CreatedAt));
}