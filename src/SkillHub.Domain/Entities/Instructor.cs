using SkillHub.Domain.Common;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.ValueObjects;

namespace SkillHub.Domain.Entities;

public class Instructor : AggregateRoot
{
    public string IdentityUserId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private readonly List<Workshop> _workshops = new();
    public IReadOnlyCollection<Workshop> Workshops => _workshops.AsReadOnly();

    protected Instructor()
    {
    }

    private Instructor(string identityUserId, string fullName, string email, string specialty, DateTime createdAtUtc)
    {
        IdentityUserId = identityUserId;
        FullName = fullName;
        Email = email;
        Specialty = specialty;
        CreatedAt = createdAtUtc;
    }

    public static Instructor Create(
        string identityUserId,
        string fullName,
        string email,
        string specialty,
        DateTime currentUtc)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
            throw new InvalidDomainValueException(nameof(IdentityUserId), "Identity user ID is required.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidDomainValueException(nameof(FullName), "Instructor full name is required.");

        if (string.IsNullOrWhiteSpace(specialty))
            throw new InvalidDomainValueException(nameof(Specialty), "Instructor specialty is required.");

        var normalizedEmail = EmailAddress.Create(email).Value;

        return new Instructor(
            identityUserId.Trim(),
            fullName.Trim(),
            normalizedEmail,
            specialty.Trim(),
            TimeSlot.NormalizeToUtc(currentUtc));
    }
}
