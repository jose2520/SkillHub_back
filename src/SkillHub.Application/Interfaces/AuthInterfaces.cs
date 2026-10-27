using SkillHub.Application.DTOs;

namespace SkillHub.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterParticipantAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(
        string identityUserId,
        Guid profileId,
        string fullName,
        string email,
        string role);
}
