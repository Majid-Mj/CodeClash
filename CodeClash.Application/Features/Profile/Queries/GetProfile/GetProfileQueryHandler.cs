using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Profile.DTOs;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Features.Profile.Queries.GetProfile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result<ProfileDto>.Failure("User not found.", "User profile not found.");
        }

        var dto = new ProfileDto(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            PhoneNumber: user.PhoneNumber,
            ProfileImageUrl: user.ProfileImageUrl,
            CreatedAt: user.CreatedAt
        );

        return Result<ProfileDto>.Success(dto, "Profile retrieved successfully.");
    }
}
