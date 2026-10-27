using FluentValidation;
using SkillHub.Application.DTOs;

namespace SkillHub.Application.Validators;

public sealed class CreateEnrollmentRequestValidator : AbstractValidator<CreateEnrollmentRequestDto>
{
    public CreateEnrollmentRequestValidator()
    {
        RuleFor(x => x.WorkshopId)
            .NotEmpty().WithMessage("WorkshopId is required.");
    }
}
