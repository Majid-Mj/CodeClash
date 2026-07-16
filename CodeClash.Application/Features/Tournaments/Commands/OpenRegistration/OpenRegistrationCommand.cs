using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.OpenRegistration;

public record OpenRegistrationCommand(Guid Id) : IRequest;
