using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Events;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Tests.Domain;

public sealed class WorkshopAndEnrollmentDomainTests
{
    private static readonly DateTime FixedNowUtc = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Workshop_CreateNew_WithPastStart_ThrowsBusinessRuleException()
    {
        var ex = Assert.Throws<BusinessRuleException>(() =>
            Workshop.CreateNew(
                "FOT-001",
                "Mobile Photography",
                "Composition and light",
                Guid.NewGuid(),
                FixedNowUtc.AddHours(-2),
                FixedNowUtc.AddHours(2),
                capacity: 15,
                currentUtc: FixedNowUtc));

        Assert.Equal("Workshop.PastStartDate", ex.ErrorCode);
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

        // Capacity reached -> CapacityExceededException
        Assert.Throws<CapacityExceededException>(() =>
            workshop.EnsureCanAcceptEnrollment(activeEnrollmentsCount: 2, currentUtc: FixedNowUtc));

        // Workshop already started -> BusinessRuleException
        var afterStart = FixedNowUtc.AddDays(5).AddMinutes(1);
        var startedEx = Assert.Throws<BusinessRuleException>(() =>
            workshop.EnsureCanAcceptEnrollment(activeEnrollmentsCount: 0, currentUtc: afterStart));
        Assert.Equal("Workshop.AlreadyStarted", startedEx.ErrorCode);
    }

    [Fact]
    public void Enrollment_Cancel_ParticipantAtLeast24HoursBeforeStart_SucceedsAndRecordsAudit()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc);
        var workshopStart = FixedNowUtc.AddHours(30);

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
        var workshopStart = FixedNowUtc.AddHours(12);

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
        var workshopStart = FixedNowUtc.AddHours(2);

        enrollment.Cancel(
            cancelledBy: "andres.mejia@correo.com",
            isEmployee: true,
            workshopStartUtc: workshopStart,
            currentUtc: FixedNowUtc);

        Assert.Equal(EnrollmentStatus.Cancelled, enrollment.Status);
        Assert.Equal("andres.mejia@correo.com", enrollment.CancelledBy);
    }

    [Fact]
    public void Enrollment_Cancel_AfterWorkshopStarted_ThrowsEvenForEmployee()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), FixedNowUtc.AddDays(-2));
        var workshopStart = FixedNowUtc.AddMinutes(-5);

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
