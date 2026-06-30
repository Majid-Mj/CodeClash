using CodeClash.Domain.Entities;

namespace CodeClash.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRawRefreshToken();
    string HashToken(string rawToken);
}