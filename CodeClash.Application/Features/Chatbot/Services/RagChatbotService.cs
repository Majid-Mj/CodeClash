using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Chatbot.Dtos;
using CodeClash.Domain.Entities;
using System.Text;

namespace CodeClash.Application.Features.Chatbot.Services;

public sealed class RagChatbotService : IRagChatbotService
{
    private readonly IProblemContextRepository _problemContextRepo;
    private readonly IChatSessionRepository _sessionRepo;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ICompletionService _completionService;

    public RagChatbotService(
        IProblemContextRepository problemContextRepo,
        IChatSessionRepository sessionRepo,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ICompletionService completionService)
    {
        _problemContextRepo = problemContextRepo;
        _sessionRepo = sessionRepo;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _completionService = completionService;
    }

    public async Task<ChatResponse> ChatAsync(Guid userId, ChatRequest request, CancellationToken ct = default)
    {
        // 1. Resolve / create session
        var session = await _sessionRepo.GetOrCreateAsync(userId, request.ProblemId, request.SessionId, ct);

        // 2. Structured retrieval (Dapper) — only if problemId provided
        ProblemContext? problemCtx = null;
        if (request.ProblemId.HasValue)
            problemCtx = await _problemContextRepo.GetByIdAsync(request.ProblemId.Value, ct);

        // 3. Vector retrieval
        var queryVec = await _embeddingService.EmbedAsync(request.Message, ct);
        var chunks = await _vectorStore.SearchAsync(queryVec, topK: 3, ct);

        // 4. Load recent history (last 10 turns = 20 messages)
        var history = await _sessionRepo.GetMessagesAsync(session.Id, limit: 20, ct);

        // 5. Build system prompt
        var systemPrompt = BuildSystemPrompt(problemCtx, chunks);

        // 6. Build history tuples for the LLM (exclude any system messages)
        var historyTuples = history
            .Select(m => (m.Role, m.Content))
            .ToList();

        // 7. Persist user message
        session.AddUserMessage(request.Message);

        // 8. Call LLM
        var reply = await _completionService.CompleteAsync(systemPrompt, historyTuples, request.Message, ct);

        // 9. Persist assistant reply
        session.AddAssistantMessage(reply);
        await _sessionRepo.SaveAsync(session, ct);

        return new ChatResponse
        {
            Reply = reply,
            SessionId = session.Id,
            SourcesUsed = chunks.Select(c => c.Title).ToList()
        };
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid sessionId, int limit = 50, CancellationToken ct = default)
        => await _sessionRepo.GetMessagesAsync(sessionId, limit, ct);

    // ── Prompt builder ────────────────────────────────────────────────────────
    private static string BuildSystemPrompt(ProblemContext? problem, IReadOnlyList<KnowledgeChunk> chunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are CodeClash Assistant — a helpful AI for competitive programmers.");
        sb.AppendLine("Answer concisely. When citing knowledge sources, use [1], [2], etc.");
        sb.AppendLine("If the user asks a general programming, debugging, or algorithmic question (such as explaining Binary Search, sorting, or data structures), you should use your general pre-trained knowledge to answer them fully. Only restrict yourself to the provided knowledge sources when answering questions specific to the CodeClash platform rules, scoring, and features.");
        sb.AppendLine();

        if (problem is not null)
        {
            sb.AppendLine("## Current Problem");
            sb.AppendLine($"**Title:** {problem.Title} ({problem.Difficulty})");
            sb.AppendLine($"**Time limit:** {problem.TimeLimitMs}ms | **Memory limit:** {problem.MemoryLimitMb}MB");
            sb.AppendLine($"**Allowed languages:** {problem.AllowedLanguagesJson}");
            sb.AppendLine($"**Constraints:** {problem.ConstraintsJson}");
            sb.AppendLine();
            sb.AppendLine("### Problem Statement");
            sb.AppendLine(problem.StatementMarkdown);
            sb.AppendLine();
        }

        if (chunks.Count > 0)
        {
            sb.AppendLine("## Relevant Knowledge");
            for (int i = 0; i < chunks.Count; i++)
                sb.AppendLine($"[{i + 1}] **{chunks[i].Title}**\n{chunks[i].Content}\n");
        }

        return sb.ToString();
    }
}
