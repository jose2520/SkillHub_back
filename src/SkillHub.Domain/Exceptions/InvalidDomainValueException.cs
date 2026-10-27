namespace SkillHub.Domain.Exceptions;

public sealed class InvalidDomainValueException : DomainException
{
    public string PropertyName { get; }

    public InvalidDomainValueException(string propertyName, string message)
        : base($"Validation.{propertyName}", message)
    {
        PropertyName = propertyName;
    }
}
