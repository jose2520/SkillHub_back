using SkillHub.Domain.Exceptions;
using SkillHub.Domain.ValueObjects;

namespace SkillHub.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void EmailAddress_Create_NormalizesTrimAndLowercase()
    {
        var email = EmailAddress.Create("  Andres.Mejia@Correo.COM ");
        Assert.Equal("andres.mejia@correo.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    public void EmailAddress_Create_InvalidFormat_ThrowsInvalidDomainValueException(string input)
    {
        Assert.Throws<InvalidDomainValueException>(() => EmailAddress.Create(input));
    }

    [Fact]
    public void WorkshopCode_Create_NormalizesUppercase()
    {
        var code = WorkshopCode.Create(" fot-001 ");
        Assert.Equal("FOT-001", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("INVALID CODE WITH SPACES")]
    public void WorkshopCode_Create_InvalidFormat_ThrowsInvalidDomainValueException(string input)
    {
        Assert.Throws<InvalidDomainValueException>(() => WorkshopCode.Create(input));
    }

    [Fact]
    public void TimeSlot_Create_EndBeforeOrEqualStart_ThrowsBusinessRuleException()
    {
        var start = new DateTime(2027, 1, 10, 10, 0, 0, DateTimeKind.Utc);
        Assert.Throws<BusinessRuleException>(() => TimeSlot.Create(start, start));
        Assert.Throws<BusinessRuleException>(() => TimeSlot.Create(start, start.AddHours(-1)));
    }

    [Fact]
    public void TimeSlot_OverlapsWith_DetectsOverlapAndAllowsAdjacentSlots()
    {
        var morning = TimeSlot.Create(
            new DateTime(2027, 1, 10, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 10, 12, 0, 0, DateTimeKind.Utc));

        var overlapping = TimeSlot.Create(
            new DateTime(2027, 1, 10, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 10, 14, 0, 0, DateTimeKind.Utc));

        var backToBackAfternoon = TimeSlot.Create(
            new DateTime(2027, 1, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 10, 16, 0, 0, DateTimeKind.Utc));

        Assert.True(morning.OverlapsWith(overlapping));
        Assert.False(morning.OverlapsWith(backToBackAfternoon));
    }
}
