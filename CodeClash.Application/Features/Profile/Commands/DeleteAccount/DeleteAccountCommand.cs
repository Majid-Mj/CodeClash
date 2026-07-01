using CodeClash.Application.Common.Models;
using MediatR;
using System;

namespace CodeClash.Application.Features.Profile.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest<Result>;
