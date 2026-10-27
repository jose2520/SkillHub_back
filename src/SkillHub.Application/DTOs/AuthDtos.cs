namespace SkillHub.Application.DTOs;

public sealed record RegisterRequestDto(
    string FullName,
    string Email,
    string Password);

public sealed record LoginRequestDto(
    string Email,
    string Password);

public sealed record AuthResponseDto(
    string Token,
    DateTime ExpiresAtUtc,
    Guid ProfileId,
    string FullName,
    string Email,
    string Role);
