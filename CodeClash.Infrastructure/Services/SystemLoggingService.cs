using CodeClash.Application.Common.Interfaces;
using CodeClash.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Infrastructure.Services;

public class SystemLoggingService : ISystemLoggingService
{
    private readonly IServiceProvider _serviceProvider;

    public SystemLoggingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task LogAsync(string level, string category, string message, string source, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a separate scope so that writing system logs is isolated and 
            // does not interfere with the active transaction or pending changes 
            // of the primary request's scoped DbContext.
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var log = new SystemLog(level, category, message, source);
            await context.SystemLogs.AddAsync(log, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Fallback to console if writing to database fails, to prevent crashing the application
            Console.WriteLine($"[SystemLoggingService Error] Failed to write system log to database: {ex.Message}");
        }
    }

    public Task LogInfoAsync(string category, string message, string source, CancellationToken cancellationToken = default)
    {
        return LogAsync("INFO", category, message, source, cancellationToken);
    }

    public Task LogWarningAsync(string category, string message, string source, CancellationToken cancellationToken = default)
    {
        return LogAsync("WARNING", category, message, source, cancellationToken);
    }

    public Task LogErrorAsync(string category, string message, string source, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var fullMessage = exception != null 
            ? $"{message} (Exception: {exception.Message} | StackTrace: {exception.StackTrace})" 
            : message;
        
        return LogAsync("ERROR", category, fullMessage, source, cancellationToken);
    }
}
