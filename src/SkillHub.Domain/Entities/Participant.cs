using SkillHub.Domain.Common;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.ValueObjects;

namespace SkillHub.Domain.Entities;

public class Participant : AggregateRoot
{
    public string IdentityUserId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private readonly List<Enrollment> _enrollments = new();
    public IReadOnlyCollection<Enrollment> Enrollments => _enrollments.AsReadOnly();

    protected Participant()
    {
    }

    private Participant(string identityUserId, string fullName, string email, DateTime createdAtUtc)
    {
        IdentityUserId = identityUserId;
        FullName = fullName;
        Email = email;
        CreatedAt = createdAtUtc;
    }

    public static Participant Create(
        string identityUserId,
        string fullName,
        string email,
        DateTime currentUtc)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
            throw new InvalidDomainValueException(nameof(IdentityUserId), "Identity user ID is required.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidDomainValueException(nameof(FullName), "Participant full name is required.");

        var normalizedEmail = EmailAddress.Create(email).Value;

        return new Participant(
            identityUserId.Trim(),
            fullName.Trim(),
            normalizedEmail,
            TimeSlot.NormalizeToUtc(currentUtc));
    }
}
