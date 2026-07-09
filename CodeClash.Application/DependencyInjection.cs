using CodeClash.Application.Common.Behaviours;
using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Features.Chatbot.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CodeClash.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        // RAG Chatbot orchestration
        services.AddScoped<IRagChatbotService, RagChatbotService>();

        return services;
    }
}