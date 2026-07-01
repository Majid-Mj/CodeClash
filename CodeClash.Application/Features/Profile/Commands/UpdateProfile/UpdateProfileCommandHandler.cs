using CodeClash.Application.Common.Interfaces;
using CodeClash.Application.Common.Models;
using CodeClash.Application.Features.Profile.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Application.Features.Profile.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<ProfileDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _context;

    public UpdateProfileCommandHandler(IUserRepository userRepository, IApplicationDbContext context)
    {
        _userRepository = userRepository;
        _context = context;
    }

    public async Task<Result<ProfileDto>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result<ProfileDto>.Failure("User not found.", "User profile not found.");
        }

        var dto = request.Dto;
        string normalizedUsername = dto.Username.Trim().ToLower();

        // Check if username is changing
        if (user.Username != normalizedUsername)
        {
            // Check if username is already taken
            bool usernameExists = await _context.Users
                .AnyAsync(u => u.Username == normalizedUsername && u.Id != request.UserId, ct);

            if (usernameExists)
            {
                return Result<ProfileDto>.Failure("Username is already taken.", "Username is already taken.");
            }
        }

        // Update domain state
        user.UpdateProfile(dto.FullName, dto.PhoneNumber, dto.Username);

        // Persist updates
        await _userRepository.UpdateAsync(user, ct);

        // Build return DTO
        var profileDto = new ProfileDto(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            PhoneNumber: user.PhoneNumber,
            ProfileImageUrl: user.ProfileImageUrl,
            CreatedAt: user.CreatedAt
        );

        return Result<ProfileDto>.Success(profileDto, "Profile updated successfully.");
    }
}
