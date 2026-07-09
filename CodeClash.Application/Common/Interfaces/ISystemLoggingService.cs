using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Common.Interfaces;

public interface ISystemLoggingService
{
    Task LogAsync(string level, string category, string message, string source, CancellationToken cancellationToken = default);
    Task LogInfoAsync(string category, string message, string source, CancellationToken cancellationToken = default);
    Task LogWarningAsync(string category, string message, string source, CancellationToken cancellationToken = default);
    Task LogErrorAsync(string category, string message, string source, Exception? exception = null, CancellationToken cancellationToken = default);
}
