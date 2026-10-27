using FluentValidation;
using SkillHub.Application.DTOs;

namespace SkillHub.Application.Validators;

public sealed class CreateWorkshopRequestValidator : AbstractValidator<CreateWorkshopRequestDto>
{
    public CreateWorkshopRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Workshop code is required.")
            .MaximumLength(30).WithMessage("Workshop code must not exceed 30 characters.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Workshop title is required.")
            .MaximumLength(200).WithMessage("Workshop title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Workshop description is required.")
            .MaximumLength(1000).WithMessage("Workshop description must not exceed 1000 characters.");

        RuleFor(x => x.InstructorEmail)
            .NotEmpty().WithMessage("Instructor email is required.")
            .EmailAddress().WithMessage("A valid instructor email address is required.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Workshop capacity must be greater than zero.");

        RuleFor(x => x.EndDateTime)
            .GreaterThan(x => x.StartDateTime)
            .WithMessage("End date and time must be strictly after start date and time.");
    }
}
