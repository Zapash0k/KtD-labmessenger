using MessengerApi.Models;
using MessengerApi.Storage;
using Microsoft.EntityFrameworkCore;

namespace MessengerApi.Services;

public class MessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db) => _db = db;

    /// Sends a message to a conversation.
    /// For group chats (Variant 4): performs fan-out by creating a
    /// per-recipient DeliveryRecord for each member except the sender.
    public async Task<MessageResponse> SendMessageAsync(SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Message text cannot be empty.");

        // Validate sender
        if (!await _db.Users.AnyAsync(u => u.Id == request.SenderId))
            throw new KeyNotFoundException($"User '{request.SenderId}' not found.");

        // Validate conversation and membership
        var conversation = await _db.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId)
            ?? throw new KeyNotFoundException($"Conversation '{request.ConversationId}' not found.");

        bool isMember = conversation.Members.Any(m => m.UserId == request.SenderId);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User '{request.SenderId}' is not a member of conversation '{request.ConversationId}'.");

        // Create message
        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            Text = request.Text.Trim(),
            Status = MessageStatus.Sent
        };
        _db.Messages.Add(message);

        // Fan-out: create DeliveryRecord for each recipient (everyone except sender)
        var recipients = conversation.Members
            .Where(m => m.UserId != request.SenderId)
            .Select(m => m.UserId)
            .ToList();

        foreach (var recipientId in recipients)
        {
            _db.DeliveryRecords.Add(new DeliveryRecord
            {
                MessageId = message.Id,
                RecipientId = recipientId,
                Status = DeliveryStatus.Pending
            });
        }

        await _db.SaveChangesAsync();
        return await LoadMessageResponseAsync(message.Id);
    }

    /// Acknowledges delivery to a specific recipient and updates
    /// the aggregated message status (Variant 4).
    public async Task<MessageResponse> AcknowledgeDeliveryAsync(AcknowledgeDeliveryRequest request)
    {
        var record = await _db.DeliveryRecords
            .FirstOrDefaultAsync(dr =>
                dr.MessageId == request.MessageId &&
                dr.RecipientId == request.RecipientId)
            ?? throw new KeyNotFoundException(
                $"No delivery record for message '{request.MessageId}' and recipient '{request.RecipientId}'.");

        if (record.Status == DeliveryStatus.Pending || record.Status == DeliveryStatus.Failed)
        {
            record.Status = DeliveryStatus.Delivered;
            record.DeliveredAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        await UpdateAggregatedStatusAsync(request.MessageId);
        return await LoadMessageResponseAsync(request.MessageId);
    }

    /// Marks a message as read for a specific recipient (Variant 4).
    public async Task<MessageResponse> MarkAsReadAsync(MarkAsReadRequest request)
    {
        var record = await _db.DeliveryRecords
            .FirstOrDefaultAsync(dr =>
                dr.MessageId == request.MessageId &&
                dr.RecipientId == request.RecipientId)
            ?? throw new KeyNotFoundException(
                $"No delivery record for message '{request.MessageId}' and recipient '{request.RecipientId}'.");

        if (record.Status != DeliveryStatus.Read)
        {
            record.Status = DeliveryStatus.Read;
            record.ReadAt = DateTime.UtcNow;
            if (record.DeliveredAt is null)
                record.DeliveredAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        await UpdateAggregatedStatusAsync(request.MessageId);
        return await LoadMessageResponseAsync(request.MessageId);
    }

    /// Returns all messages in a conversation with delivery details.
    public async Task<List<MessageResponse>> GetMessagesAsync(string conversationId)
    {
        if (!await _db.Conversations.AnyAsync(c => c.Id == conversationId))
            throw new KeyNotFoundException($"Conversation '{conversationId}' not found.");

        var messageIds = await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => m.Id)
            .ToListAsync();

        var responses = new List<MessageResponse>();
        foreach (var id in messageIds)
            responses.Add(await LoadMessageResponseAsync(id));
        return responses;
    }

    //Private helpers

    /// Recalculates the aggregated message status based on all DeliveryRecords.
    /// Logic mirrors the State Diagram from Lab 1 (Variant 4).
    private async Task UpdateAggregatedStatusAsync(string messageId)
    {
        var message = await _db.Messages.FindAsync(messageId);
        if (message is null) return;

        var records = await _db.DeliveryRecords
            .Where(dr => dr.MessageId == messageId)
            .ToListAsync();

        if (records.Count == 0)
        {
            // No recipients (sender is alone) → treat as delivered
            message.Status = MessageStatus.Delivered;
        }
        else
        {
            bool allRead = records.All(r => r.Status == DeliveryStatus.Read);
            bool anyRead = records.Any(r => r.Status == DeliveryStatus.Read);
            bool allDeliveredOrRead = records.All(r =>
                r.Status == DeliveryStatus.Delivered || r.Status == DeliveryStatus.Read);
            bool anyDeliveredOrRead = records.Any(r =>
                r.Status == DeliveryStatus.Delivered || r.Status == DeliveryStatus.Read);

            message.Status = (allRead, anyRead, allDeliveredOrRead, anyDeliveredOrRead) switch
            {
                (true, _, _, _) => MessageStatus.Read,
                (false, true, true, _) => MessageStatus.PartiallyRead,
                (false, true, false, _) => MessageStatus.PartiallyRead,
                (false, false, true, _) => MessageStatus.Delivered,
                (false, false, false, true) => MessageStatus.PartiallyDelivered,
                _ => MessageStatus.Sent
            };
        }

        await _db.SaveChangesAsync();
    }

    private async Task<MessageResponse> LoadMessageResponseAsync(string messageId)
    {
        var message = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.DeliveryRecords)
            .ThenInclude(dr => dr.Recipient)
            .FirstOrDefaultAsync(m => m.Id == messageId)
            ?? throw new KeyNotFoundException($"Message '{messageId}' not found.");

        var deliveryRecords = message.DeliveryRecords
            .OrderBy(dr => dr.CreatedAt)
            .Select(dr => new DeliveryRecordResponse(
                dr.RecipientId,
                dr.Recipient.Name,
                dr.Status.ToString(),
                dr.DeliveredAt,
                dr.ReadAt))
            .ToList();

        return new MessageResponse(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Sender.Name,
            message.Text,
            message.Status.ToString(),
            message.CreatedAt,
            deliveryRecords);
    }
}