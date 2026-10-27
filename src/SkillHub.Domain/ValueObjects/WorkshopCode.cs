using System.Text.RegularExpressions;
using SkillHub.Domain.Common;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.ValueObjects;

public sealed partial class WorkshopCode : ValueObject
{
    public string Value { get; }

    private WorkshopCode(string value)
    {
        Value = value;
    }

    public static WorkshopCode Create(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            throw new InvalidDomainValueException(nameof(WorkshopCode), "Workshop code is required.");

        var normalized = rawCode.Trim().ToUpperInvariant();
        if (normalized.Length > 30 || !WorkshopCodeRegex().IsMatch(normalized))
            throw new InvalidDomainValueException(
                nameof(WorkshopCode),
                $"Workshop code '{rawCode}' is invalid. Expected format such as 'FOT-001'.");

        return new WorkshopCode(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9\-_]{1,29}$", RegexOptions.CultureInvariant)]
    private static partial Regex WorkshopCodeRegex();
}
