using MessengerApi.Models;

namespace MessengerApi.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ConversationMember> ConversationMemberships { get; set; } = new List<ConversationMember>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<DeliveryRecord> DeliveryRecords { get; set; } = new List<DeliveryRecord>();
}