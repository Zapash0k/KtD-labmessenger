using MessengerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessengerApi.Api;

[ApiController]
[Route("conversations")]
public class ConversationsController : ControllerBase
{
    private readonly ConversationService _conversations;
    private readonly MessageService _messages;

    public ConversationsController(ConversationService conversations, MessageService messages)
    {
        _conversations = conversations;
        _messages = messages;
    }

    //Create a 1-on-1 direct conversation.
    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirect([FromBody] CreateDirectConversationRequest request)
    {
        var conversation = await _conversations.CreateDirectAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = conversation.Id }, conversation);
    }

    //Create a group conversation (Variant 4).
    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupConversationRequest request)
    {
        var conversation = await _conversations.CreateGroupAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = conversation.Id }, conversation);
    }

    //Get a conversation by ID.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) =>
        Ok(await _conversations.GetConversationAsync(id));

    //Get all conversations for a specific user.
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserConversations(string userId) =>
        Ok(await _conversations.GetUserConversationsAsync(userId));

    //Get all messages in a conversation.
    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(string id) =>
        Ok(await _messages.GetMessagesAsync(id));
}