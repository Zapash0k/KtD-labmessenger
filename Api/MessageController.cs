using MessengerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessengerApi.Api;

[ApiController]
[Route("messages")]
public class MessagesController : ControllerBase
{
    private readonly MessageService _messages;

    public MessagesController(MessageService messages) => _messages = messages;

    //Send a message to a conversation.
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        var message = await _messages.SendMessageAsync(request);
        return StatusCode(202, message);
    }
    /// Acknowledge delivery of a message for a specific recipient (Variant 4).
    /// Called by the client when a message is received.
    [HttpPost("{messageId}/deliver")]
    public async Task<IActionResult> AcknowledgeDelivery(
        string messageId,
        [FromBody] AcknowledgeDeliveryRequest request)
    {
        var updated = request with { MessageId = messageId };
        var message = await _messages.AcknowledgeDeliveryAsync(updated);
        return Ok(message);
    }
    /// Mark a message as read for a specific recipient (Variant 4).
    /// Called when the recipient views the message.
    [HttpPost("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(
        string messageId,
        [FromBody] MarkAsReadRequest request)
    {
        var updated = request with { MessageId = messageId };
        var message = await _messages.MarkAsReadAsync(updated);
        return Ok(message);
    }
}