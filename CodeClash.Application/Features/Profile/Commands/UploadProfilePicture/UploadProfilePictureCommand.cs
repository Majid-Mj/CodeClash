using CodeClash.Application.Common.Models;
using MediatR;
using System;
using System.IO;

namespace CodeClash.Application.Features.Profile.Commands.UploadProfilePicture;

public record UploadProfilePictureCommand(
    Guid UserId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileLength
) : IRequest<Result<string>>;
