using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Events;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Tests.Domain;

public sealed class WorkshopAndEnrollmentDomainTests
{
    private static readonly DateTime FixedNowUtc = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Instructor_Create_ValidatesInvariantsAndNormalizesValues()
    {
        var instructor = Instructor.Create(
            " id-123 ",
            " Andrés Felipe Mejía ",
            " Andres.Mejia@Correo.com ",
            " Fotografía ",
            FixedNowUtc);

        Assert.Equal("id-123", instructor.IdentityUserId);
        Assert.Equal("Andrés Felipe Mejía", instructor.FullName);
        Assert.Equal("andres.mejia@correo.com", instructor.Email);
        Assert.Equal("Fotografía", instructor.Specialty);

        Assert.Throws<InvalidDomainValueException>(() => Instructor.Create("", "Name", "a@b.co", "Spec", FixedNowUtc));
        Assert.Throws<InvalidDomainValueException>(() => Instructor.Create("id", "", "a@b.co", "Spec", FixedNowUtc));
        Assert.Throws<InvalidDomainValueException>(() => Instructor.Create("id", "Name", "a@b.co", "", FixedNowUtc));
    }

    [Fact]
    public void Participant_Create_ValidatesInvariantsAndNormalizesValues()
    {
        var participant = Participant.Create(
            " user-456 ",
            " Jorge Pérez ",
            " JORGE.PEREZ@CORREO.COM ",
            FixedNowUtc);

        Assert.Equal("user-456", participant.IdentityUserId);
        Assert.Equal("Jorge Pérez", participant.FullName);
        Assert.Equal("jorge.perez@correo.com", participant.Email);

        Assert.Throws<InvalidDomainValueException>(() => Participant.Create("", "Jorge", "j@c.com", FixedNowUtc));
        Assert.Throws<InvalidDomainValueException>(() => Participant.Create("u1", "   ", "j@c.com", FixedNowUtc));
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(0)] // Exact current time is not future
    public void Workshop_CreateNew_WithPastOrCurrentStart_ThrowsBusinessRuleException(int offsetMinutes)
    {
        var ex = Assert.Throws<BusinessRuleException>(() =>
            Workshop.CreateNew(
                "FOT-001",
                "Mobile Photography",
                "Composition and light",
                Guid.NewGuid(),
                FixedNowUtc.AddMinutes(offsetMinutes),
                FixedNowUtc.AddHours(2),
                capacity: 15,
                currentUtc: FixedNowUtc));

        Assert.Equal("Workshop.PastStartDate", ex.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Workshop_CreateNew_NonPositiveCapacity_ThrowsBusinessRuleException(int invalidCapacity)
    {
        var ex = Assert.Throws<BusinessRuleException>(() =>
            Workshop.CreateNew(
                "FOT-001",
                "Mobile Photography",
                "Composition and light",
                Guid.NewGuid(),
                FixedNowUtc.AddDays(3),
                FixedNowUtc.AddDays(3).AddHours(2),
                capacity: invalidCapacity,
                currentUtc: FixedNowUtc));

        Assert.Equal("Workshop.InvalidCapacity", ex.ErrorCode);
    }

    [Fact]
    public void Workshop_CreateNew_MissingFields_ThrowsInvalidDomainValueException()
    {
        Assert.Throws<InvalidDomainValueException>(() =>
            Workshop.CreateNew("FOT-001", "", "Desc", Guid.NewGuid(), FixedNowUtc.AddDays(2), FixedNowUtc.AddDays(2).AddHours(2), 10, FixedNowUtc));

        Assert.Throws<InvalidDomainValueException>(() =>
            Workshop.CreateNew("FOT-001", "Title", "", Guid.NewGuid(), FixedNowUtc.AddDays(2), FixedNowUtc.AddDays(2).AddHours(2), 10, FixedNowUtc));

        Assert.Throws<InvalidDomainValueException>(() =>
            Workshop.CreateNew("FOT-001", "Title", "Desc", Guid.Empty, FixedNowUtc.AddDays(2), FixedNowUtc.AddDays(2).AddHours(2), 10, FixedNowUtc));
    }

    [Fact]
    public void Workshop_EnsureCanAcceptEnrollment_EnforcesCapacityAndFutureStart()
    {
        var workshop = Workshop.CreateNew(
            "FOT-001",
            "Mobile Photography",
            "Composition and light",
            Guid.NewGuid(),
            FixedNowUtc.AddDays(5),
            FixedNowUtc.AddDays(5).AddHours(4),
            capacity: 2,
            currentUtc: FixedNowUtc);

        Assert.Single(workshop.DomainEvents.OfType<WorkshopCreatedEvent>());
        Assert.Equal(1, workshop.CalculateAvailableSpots(1));
        Assert.Equal(0, workshop.CalculateAvailableSpots(5));

        workshop.ClearDomainEvents();
        Assert.Empty(workshop.DomainEvents);

        // Capacity reached -> CapacityExceededException
        var capEx = Assert.Throws<CapacityExceededException>(() =>
            workshop.EnsureCanAcceptEnrollment(activeEnrollmentsCount: 2, currentUtc: FixedNowUtc));
        Assert.Equal("FOT-001", capEx.WorkshopCode);
        Assert.Equal(2, capEx.Capacity);

        // Workshop already started -> BusinessRuleException
        var afterStart = FixedNowUtc.AddDays(5);
        var startedEx = Assert.Throws<BusinessRuleException>(() =>
            workshop.EnsureCanAcceptEnrollment(activeEnrollmentsCount: 0, currentUtc: afterStart));
        Assert.Equal("Workshop.AlreadyStarted", startedEx.ErrorCode);
    }

    [Fact]
    public void Enrollment_Create_ValidatesIdsAndStartsActive()
    {
        Assert.Throws<InvalidDomainValueException>(() => Enrollment.Create(Guid.Empty, Guid.NewGuid(), FixedNowUtc));
        Assert.Throws<InvalidDomainValueException>(() => Enrollment.Create(Guid.NewGuid(), Guid.Empty, FixedNowUtc));

        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
        Assert.Null(enrollment.CancelledAt);
        Assert.Null(enrollment.CancelledBy);
        Assert.Single(enrollment.DomainEvents.OfType<EnrollmentCreatedEvent>());
    }

    [Fact]
    public void Enrollment_Cancel_ParticipantExactly24HoursBeforeStart_SucceedsAndRecordsAudit()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc);
        var workshopStart = FixedNowUtc.AddHours(24); // Exact 24h boundary allowed ("up to 24 hours before")

        enrollment.Cancel(
            cancelledBy: "jorge.perez@correo.com",
            isEmployee: false,
            workshopStartUtc: workshopStart,
            currentUtc: FixedNowUtc);

        Assert.Equal(EnrollmentStatus.Cancelled, enrollment.Status);
        Assert.Equal("jorge.perez@correo.com", enrollment.CancelledBy);
        Assert.Equal(FixedNowUtc, enrollment.CancelledAt);
        Assert.Contains(enrollment.DomainEvents, e => e is EnrollmentCancelledEvent);
    }

    [Fact]
    public void Enrollment_Cancel_ParticipantUnder24HoursBeforeStart_ThrowsWindowClosed()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc);
        var workshopStart = FixedNowUtc.AddHours(24).AddSeconds(-1); // 23h 59m 59s -> blocked for participant

        var ex = Assert.Throws<BusinessRuleException>(() =>
            enrollment.Cancel(
                cancelledBy: "jorge.perez@correo.com",
                isEmployee: false,
                workshopStartUtc: workshopStart,
                currentUtc: FixedNowUtc));

        Assert.Equal("Enrollment.CancellationWindowClosed", ex.ErrorCode);
    }

    [Fact]
    public void Enrollment_Cancel_EmployeeUnder24HoursBeforeStart_Succeeds()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc);
        var workshopStart = FixedNowUtc.AddMinutes(30);

        enrollment.Cancel(
            cancelledBy: "andres.mejia@correo.com",
            isEmployee: true,
            workshopStartUtc: workshopStart,
            currentUtc: FixedNowUtc);

        Assert.Equal(EnrollmentStatus.Cancelled, enrollment.Status);
        Assert.Equal("andres.mejia@correo.com", enrollment.CancelledBy);
    }

    [Fact]
    public void Enrollment_Cancel_AlreadyCancelled_ThrowsAlreadyCancelled()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc);
        var workshopStart = FixedNowUtc.AddDays(3);

        enrollment.Cancel("andres.mejia@correo.com", isEmployee: true, workshopStart, FixedNowUtc);

        var ex = Assert.Throws<BusinessRuleException>(() =>
            enrollment.Cancel("andres.mejia@correo.com", isEmployee: true, workshopStart, FixedNowUtc));

        Assert.Equal("Enrollment.AlreadyCancelled", ex.ErrorCode);
    }

    [Fact]
    public void Enrollment_Cancel_AtOrAfterWorkshopStarted_ThrowsEvenForEmployee()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc.AddDays(-2));
        var workshopStart = FixedNowUtc;

        var ex = Assert.Throws<BusinessRuleException>(() =>
            enrollment.Cancel(
                cancelledBy: "andres.mejia@correo.com",
                isEmployee: true,
                workshopStartUtc: workshopStart,
                currentUtc: FixedNowUtc));

        Assert.Equal("Enrollment.WorkshopAlreadyStarted", ex.ErrorCode);
    }

    [Fact]
    public void Enrollment_EnsureCanGenerateCertificate_CancelledThrowsBusinessRuleException()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc);
        enrollment.EnsureCanGenerateCertificate();

        enrollment.Cancel("andres.mejia@correo.com", isEmployee: true, FixedNowUtc.AddDays(3), FixedNowUtc);

        var ex = Assert.Throws<BusinessRuleException>(() => enrollment.EnsureCanGenerateCertificate());
        Assert.Equal("Enrollment.CertificateRequiresActive", ex.ErrorCode);
    }
}
