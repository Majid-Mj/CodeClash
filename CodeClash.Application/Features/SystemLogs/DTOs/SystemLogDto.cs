using System;

namespace CodeClash.Application.Features.SystemLogs.DTOs;

public record SystemLogDto(
    Guid Id,
    string Level,
    string Category,
    string Message,
    string Source,
    DateTime CreatedAt
);
