using MessengerApi.Models;

namespace MessengerApi.Models;

public enum ConversationType
{
    Direct,
    Group
}

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ConversationType Type { get; set; }
    public string? Name { get; set; } // For group chats
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}