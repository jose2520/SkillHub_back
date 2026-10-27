using System.Text.RegularExpressions;
using SkillHub.Domain.Common;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.ValueObjects;

public sealed partial class EmailAddress : ValueObject
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string? rawEmail)
    {
        if (string.IsNullOrWhiteSpace(rawEmail))
            throw new InvalidDomainValueException(nameof(EmailAddress), "Email address is required.");

        var normalized = rawEmail.Trim().ToLowerInvariant();
        if (normalized.Length > 256 || !EmailRegex().IsMatch(normalized))
            throw new InvalidDomainValueException(nameof(EmailAddress), $"'{rawEmail}' is not a valid email address.");

        return new EmailAddress(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
