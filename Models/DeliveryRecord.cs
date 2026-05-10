namespace MessengerApi.Models;

public enum DeliveryStatus
{
    Pending,
    Delivered,
    Read,
    Failed
}

/// Per-recipient delivery record (Variant 4 – Group Chat fan-out).
/// One record exists for each recipient of a message (excluding sender).
public class DeliveryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MessageId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation
    public Message Message { get; set; } = null!;
    public User Recipient { get; set; } = null!;
}