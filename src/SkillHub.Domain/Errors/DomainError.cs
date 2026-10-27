namespace SkillHub.Domain.Errors;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4
}

public sealed record DomainError(string Code, string Description, ErrorType Type)
{
    public static readonly DomainError None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static DomainError Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static DomainError NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static DomainError Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static DomainError Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static DomainError Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);
}
