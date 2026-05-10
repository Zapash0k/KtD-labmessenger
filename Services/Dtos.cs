namespace MessengerApi.Services;

//Request DTOs

public record CreateUserRequest(string Name);

public record CreateDirectConversationRequest(string UserIdA, string UserIdB);

public record CreateGroupConversationRequest(string Name, List<string> MemberIds);

public record SendMessageRequest(string ConversationId, string SenderId, string Text);

public record AcknowledgeDeliveryRequest(string MessageId, string RecipientId);

public record MarkAsReadRequest(string MessageId, string RecipientId);

//Response DTOs

public record UserResponse(string Id, string Name, DateTime CreatedAt);

public record ConversationResponse(
    string Id,
    string Type,
    string? Name,
    List<UserResponse> Members,
    DateTime CreatedAt);

public record DeliveryRecordResponse(
    string RecipientId,
    string RecipientName,
    string Status,
    DateTime? DeliveredAt,
    DateTime? ReadAt);

public record MessageResponse(
    string Id,
    string ConversationId,
    string SenderId,
    string SenderName,
    string Text,
    string Status,
    DateTime CreatedAt,
    List<DeliveryRecordResponse> DeliveryRecords);