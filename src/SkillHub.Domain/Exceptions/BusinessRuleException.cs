using SkillHub.Domain.Errors;

namespace SkillHub.Domain.Exceptions;

public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string errorCode, string message)
        : base(errorCode, message)
    {
    }

    public BusinessRuleException(DomainError error)
        : base(error.Code, error.Description)
    {
    }
}
