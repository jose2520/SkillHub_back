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
        Assert.Equal("andres.mejia@correo.com", email.ToString());
    }

    [Fact]
    public void EmailAddress_StructuralEquality_WorksCorrectly()
    {
        var a = EmailAddress.Create("JORGE.PEREZ@CORREO.COM");
        var b = EmailAddress.Create(" jorge.perez@correo.com ");
        var c = EmailAddress.Create("rosa.barrios@correo.com");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.True(a != c);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    [InlineData("user name@domain.com")]
    public void EmailAddress_Create_InvalidFormat_ThrowsInvalidDomainValueException(string? input)
    {
        var ex = Assert.Throws<InvalidDomainValueException>(() => EmailAddress.Create(input));
        Assert.Equal("EmailAddress", ex.PropertyName);
    }

    [Fact]
    public void EmailAddress_Create_ExceedingMaxLength_ThrowsInvalidDomainValueException()
    {
        var oversized = new string('a', 250) + "@correo.com";
        Assert.Throws<InvalidDomainValueException>(() => EmailAddress.Create(oversized));
    }

    [Fact]
    public void WorkshopCode_Create_NormalizesUppercaseAndStructuralEquality()
    {
        var code1 = WorkshopCode.Create(" fot-001 ");
        var code2 = WorkshopCode.Create("FOT-001");
        var code3 = WorkshopCode.Create("COC-001");

        Assert.Equal("FOT-001", code1.Value);
        Assert.Equal("FOT-001", code1.ToString());
        Assert.Equal(code1, code2);
        Assert.True(code1 == code2);
        Assert.NotEqual(code1, code3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("-FOT001")]
    [InlineData("INVALID CODE WITH SPACES")]
    [InlineData("FOT@001")]
    [InlineData("THIS_WORKSHOP_CODE_IS_WAY_TOO_LONG_EXCEEDING_30_CHARS")]
    public void WorkshopCode_Create_InvalidFormat_ThrowsInvalidDomainValueException(string? input)
    {
        var ex = Assert.Throws<InvalidDomainValueException>(() => WorkshopCode.Create(input));
        Assert.Equal("WorkshopCode", ex.PropertyName);
    }

    [Fact]
    public void TimeSlot_Create_EndBeforeOrEqualStart_ThrowsBusinessRuleException()
    {
        var start = new DateTime(2027, 1, 10, 10, 0, 0, DateTimeKind.Utc);
        var equalEx = Assert.Throws<BusinessRuleException>(() => TimeSlot.Create(start, start));
        var beforeEx = Assert.Throws<BusinessRuleException>(() => TimeSlot.Create(start, start.AddHours(-1)));

        Assert.Equal("Workshop.InvalidSchedule", equalEx.ErrorCode);
        Assert.Equal("Workshop.InvalidSchedule", beforeEx.ErrorCode);
    }

    [Fact]
    public void TimeSlot_NormalizeToUtc_HandlesUnspecifiedAndUtcKinds()
    {
        var unspecifiedStart = new DateTime(2027, 2, 15, 9, 0, 0, DateTimeKind.Unspecified);
        var unspecifiedEnd = new DateTime(2027, 2, 15, 12, 0, 0, DateTimeKind.Unspecified);

        var slot = TimeSlot.Create(unspecifiedStart, unspecifiedEnd);

        Assert.Equal(DateTimeKind.Utc, slot.StartDateTime.Kind);
        Assert.Equal(DateTimeKind.Utc, slot.EndDateTime.Kind);
    }

    [Theory]
    [InlineData(8, 12, 11, 14, true)]   // Overlaps at end
    [InlineData(10, 14, 8, 11, true)]   // Overlaps at start
    [InlineData(8, 18, 10, 12, true)]   // Contains inner slot
    [InlineData(10, 12, 8, 18, true)]   // Contained within outer slot
    [InlineData(8, 12, 8, 12, true)]    // Exact same slot
    [InlineData(8, 12, 12, 16, false)]  // Back-to-back (end == next start)
    [InlineData(12, 16, 8, 12, false)]  // Back-to-back reverse
    [InlineData(8, 10, 14, 16, false)]  // Completely disjoint
    public void TimeSlot_OverlapsWith_EvaluatesAllIntervalCasesCorrectly(
        int startA, int endA, int startB, int endB, bool expectedOverlap)
    {
        var slotA = TimeSlot.Create(
            new DateTime(2027, 1, 10, startA, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 10, endA, 0, 0, DateTimeKind.Utc));

        var slotB = TimeSlot.Create(
            new DateTime(2027, 1, 10, startB, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 10, endB, 0, 0, DateTimeKind.Utc));

        Assert.Equal(expectedOverlap, slotA.OverlapsWith(slotB));
    }
}
