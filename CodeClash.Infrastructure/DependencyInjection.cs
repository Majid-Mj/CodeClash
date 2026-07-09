using CodeClash.Application.Common.Interfaces;
using CodeClash.Infrastructure.Chatbot;
using CodeClash.Infrastructure.Persistence;
using CodeClash.Infrastructure.Persistence.Repositories;
using CodeClash.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeClash.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        // Repositories
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IDockerExecutionService, DockerExecutionService>();
        services.AddScoped<ISystemLoggingService, SystemLoggingService>();
        
        services.AddHttpClient<IAIProvider, CodeClash.Infrastructure.Services.AI.GeminiProvider>();

        services.AddScoped<IProblemContextRepository, ProblemContextRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddSingleton<ICompletionService, OpenAiCompletionService>();

        services.AddScoped<IVectorStore, SqlVectorStore>();

        return services;
    }
}