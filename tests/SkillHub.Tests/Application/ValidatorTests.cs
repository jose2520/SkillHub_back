using SkillHub.Application.DTOs;
using SkillHub.Application.Validators;

namespace SkillHub.Tests.Application;

public sealed class ValidatorTests
{
    [Fact]
    public void RegisterRequestValidator_ValidatesAllFields()
    {
        var validator = new RegisterRequestValidator();

        var valid = validator.Validate(new RegisterRequestDto("Jose Diaz", "jose@correo.com", "Secret123*"));
        Assert.True(valid.IsValid);

        var invalid = validator.Validate(new RegisterRequestDto("", "not-an-email", "123"));
        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, e => e.PropertyName == nameof(RegisterRequestDto.FullName));
        Assert.Contains(invalid.Errors, e => e.PropertyName == nameof(RegisterRequestDto.Email));
        Assert.Contains(invalid.Errors, e => e.PropertyName == nameof(RegisterRequestDto.Password));
    }

    [Fact]
    public void LoginRequestValidator_ValidatesEmailAndPassword()
    {
        var validator = new LoginRequestValidator();

        Assert.True(validator.Validate(new LoginRequestDto("andres.mejia@correo.com", "Instructor2026*")).IsValid);
        Assert.False(validator.Validate(new LoginRequestDto("bad-email", "")).IsValid);
    }

    [Fact]
    public void CreateWorkshopRequestValidator_ValidatesScheduleCapacityAndMetadata()
    {
        var validator = new CreateWorkshopRequestValidator();
        var start = new DateTime(2027, 5, 1, 9, 0, 0, DateTimeKind.Utc);

        var valid = validator.Validate(new CreateWorkshopRequestDto(
            "FOT-001", "Title", "Description", "andres.mejia@correo.com", start, start.AddHours(3), 15));
        Assert.True(valid.IsValid);

        var invalid = validator.Validate(new CreateWorkshopRequestDto(
            "", "", "", "bad-email", start, start.AddHours(-1), 0));
        Assert.False(invalid.IsValid);
        Assert.True(invalid.Errors.Count >= 6);
    }

    [Fact]
    public void CreateEnrollmentRequestValidator_RejectsEmptyGuid()
    {
        var validator = new CreateEnrollmentRequestValidator();

        Assert.False(validator.Validate(new CreateEnrollmentRequestDto(Guid.Empty)).IsValid);
        Assert.True(validator.Validate(new CreateEnrollmentRequestDto(Guid.NewGuid())).IsValid);
    }
}
