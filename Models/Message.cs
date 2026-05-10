using MessengerApi.Models;

namespace MessengerApi.Models;

public enum MessageStatus
{
    Sent,
    PartiallyDelivered,
    Delivered,
    PartiallyRead,
    Read
}

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ICollection<DeliveryRecord> DeliveryRecords { get; set; } = new List<DeliveryRecord>();
}