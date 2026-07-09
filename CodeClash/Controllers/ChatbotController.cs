using CodeClash.API.Common;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Chatbot.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CodeClash.API.Hubs;
using System.Security.Claims;

namespace CodeClash.API.Controllers;

[ApiController]
[Route("api/v1/chatbot")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IRagChatbotService _chatbot;
    private readonly IHubContext<NotificationHub> _hub;

    public ChatbotController(IRagChatbotService chatbot, IHubContext<NotificationHub> hub)
    {
        _chatbot = chatbot;
        _hub = hub;
    }

    /// <summary>Send a message to the RAG chatbot.</summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(ApiResponse<object>.Fail("Message cannot be empty."));

        var response = await _chatbot.ChatAsync(userId, request, ct);

        // Push reply to user's SignalR connection (optional live update)
        await _hub.Clients.User(userId.ToString())
            .SendAsync("ChatMessageReceived", new
            {
                sessionId = response.SessionId,
                role = "assistant",
                content = response.Reply,
                sourcesUsed = response.SourcesUsed
            }, ct);

        return Ok(ApiResponse<ChatResponse>.Ok(response, "Reply generated."));
    }

    /// <summary>Get chat history for a session.</summary>
    [HttpGet("history/{sessionId:guid}")]
    public async Task<IActionResult> GetHistory(Guid sessionId, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var messages = await _chatbot.GetHistoryAsync(sessionId, limit, ct);
        return Ok(ApiResponse<IReadOnlyList<ChatMessageDto>>.Ok(messages));
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
