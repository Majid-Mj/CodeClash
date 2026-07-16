using MediatR;

namespace CodeClash.Application.Features.Tournaments.Commands.CloseRegistration;

public record CloseRegistrationCommand(Guid Id) : IRequest;
