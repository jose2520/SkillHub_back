using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.ValueObjects;
using SkillHub.Infrastructure.Persistence;
using ValidationException = SkillHub.Application.Exceptions.ValidationException;

namespace SkillHub.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IParticipantRepository _participantRepository;
    private readonly IInstructorRepository _instructorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;

    public AuthService(
        UserManager<IdentityUser> userManager,
        IParticipantRepository participantRepository,
        IInstructorRepository instructorRepository,
        IUnitOfWork unitOfWork,
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator)
    {
        _userManager = userManager;
        _participantRepository = participantRepository;
        _instructorRepository = instructorRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponseDto> RegisterParticipantAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var normalizedEmail = EmailAddress.Create(request.Email).Value;

        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null
            || await _participantRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException($"An account with email '{normalizedEmail}' is already registered.");
        }

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var identityUser = new IdentityUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(identityUser, request.Password);
        if (!createResult.Succeeded)
        {
            var identityErrors = createResult.Errors
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Code) ? "Password" : e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new ValidationException(identityErrors);
        }

        var roleResult = await _userManager.AddToRoleAsync(identityUser, RoleNames.Participant);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException("Failed to assign Participant role to newly registered user.");
        }

        var participant = Participant.Create(
            identityUser.Id,
            request.FullName,
            normalizedEmail,
            _dateTimeProvider.UtcNow);

        await _participantRepository.AddAsync(participant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        await _emailService.SendWelcomeEmailAsync(
            participant.Email,
            participant.FullName,
            cancellationToken);

        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(
            identityUser.Id,
            participant.Id,
            participant.FullName,
            participant.Email,
            RoleNames.Participant);

        return new AuthResponseDto(
            token,
            expiresAtUtc,
            participant.Id,
            participant.FullName,
            participant.Email,
            RoleNames.Participant);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var normalizedEmail = EmailAddress.Create(request.Email).Value;
        var identityUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (identityUser is null)
            throw new UnauthorizedException("Invalid email or password.");

        if (await _userManager.IsLockedOutAsync(identityUser))
            throw new UnauthorizedException("This account is temporarily locked due to multiple failed sign-in attempts.");

        var passwordValid = await _userManager.CheckPasswordAsync(identityUser, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(identityUser);
            throw new UnauthorizedException("Invalid email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(identityUser);

        var roles = await _userManager.GetRolesAsync(identityUser);
        if (roles.Contains(RoleNames.Employee))
        {
            var instructor = await _instructorRepository.GetByIdentityUserIdAsync(identityUser.Id, cancellationToken)
                ?? await _instructorRepository.GetByEmailAsync(normalizedEmail, cancellationToken)
                ?? throw new UnauthorizedException("Instructor profile is missing for this employee account.");

            var (empToken, empExpires) = _jwtTokenService.GenerateToken(
                identityUser.Id,
                instructor.Id,
                instructor.FullName,
                instructor.Email,
                RoleNames.Employee);

            return new AuthResponseDto(
                empToken,
                empExpires,
                instructor.Id,
                instructor.FullName,
                instructor.Email,
                RoleNames.Employee);
        }

        var participant = await _participantRepository.GetByIdentityUserIdAsync(identityUser.Id, cancellationToken)
            ?? await _participantRepository.GetByEmailAsync(normalizedEmail, cancellationToken)
            ?? throw new UnauthorizedException("Participant profile is missing for this account.");

        var (partToken, partExpires) = _jwtTokenService.GenerateToken(
            identityUser.Id,
            participant.Id,
            participant.FullName,
            participant.Email,
            RoleNames.Participant);

        return new AuthResponseDto(
            partToken,
            partExpires,
            participant.Id,
            participant.FullName,
            participant.Email,
            RoleNames.Participant);
    }
}
