using System.Reflection;
using Moq;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces;
using SkillHub.Application.Services;
using SkillHub.Application.Validators;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Tests.Application;

public sealed class EnrollmentServiceTests
{
    private static readonly DateTime FixedNowUtc = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEnrollmentRepository> _enrollmentRepo = new();
    private readonly Mock<IWorkshopRepository> _workshopRepo = new();
    private readonly Mock<IParticipantRepository> _participantRepo = new();
    private readonly Mock<IExcelImportService> _excelService = new();
    private readonly Mock<ICertificatePdfService> _pdfService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public EnrollmentServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNowUtc);
    }

    private EnrollmentService CreateSut() =>
        new(
            _enrollmentRepo.Object,
            _workshopRepo.Object,
            _participantRepo.Object,
            _excelService.Object,
            _pdfService.Object,
            _emailService.Object,
            _unitOfWork.Object,
            _clock.Object,
            new CreateEnrollmentRequestValidator());

    [Fact]
    public async Task EnrollAuthenticatedParticipant_DuplicateActive_ThrowsDuplicateEnrollmentException()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "FOT-001", "Photography", "Basics", Guid.NewGuid(),
            FixedNowUtc.AddDays(10), FixedNowUtc.AddDays(10).AddHours(4), 15, FixedNowUtc);

        _participantRepo.Setup(r => r.GetByIdentityUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _workshopRepo.Setup(r => r.GetByIdWithDetailsAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);
        _enrollmentRepo.Setup(r => r.HasActiveEnrollmentAsync(participant.Id, workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<DuplicateEnrollmentException>(() =>
            sut.EnrollAuthenticatedParticipantAsync("user-1", new CreateEnrollmentRequestDto(workshop.Id)));
    }

    [Fact]
    public async Task EnrollAuthenticatedParticipant_Success_DispatchesConfirmationEmail()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var instructor = Instructor.Create("inst-1", "Prof. Gomez", "prof.gomez@skillhub.internal", "Bio", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "FOT-001", "Photography", "Basics", instructor.Id,
            FixedNowUtc.AddDays(10), FixedNowUtc.AddDays(10).AddHours(4), 15, FixedNowUtc);
        SetNavigationProperty(workshop, nameof(Workshop.Instructor), instructor);

        _participantRepo.Setup(r => r.GetByIdentityUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _workshopRepo.Setup(r => r.GetByIdWithDetailsAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);
        _enrollmentRepo.Setup(r => r.HasActiveEnrollmentAsync(participant.Id, workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _enrollmentRepo.Setup(r => r.CountActiveByWorkshopIdAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _enrollmentRepo.Setup(r => r.GetActiveByParticipantIdWithWorkshopAsync(participant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Enrollment>());

        var sut = CreateSut();
        var result = await sut.EnrollAuthenticatedParticipantAsync("user-1", new CreateEnrollmentRequestDto(workshop.Id));

        Assert.NotNull(result);
        Assert.Equal(workshop.Id, result.WorkshopId);
        _emailService.Verify(e => e.SendEnrollmentConfirmationAsync(
            "jorge.perez@correo.com",
            "Jorge Perez",
            "Photography",
            "FOT-001",
            workshop.StartDateTime,
            workshop.EndDateTime,
            "Prof. Gomez",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrollAuthenticatedParticipant_FullCapacity_ThrowsCapacityExceededException()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "FOT-001", "Photography", "Basics", Guid.NewGuid(),
            FixedNowUtc.AddDays(10), FixedNowUtc.AddDays(10).AddHours(4), capacity: 2, FixedNowUtc);

        _participantRepo.Setup(r => r.GetByIdentityUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _workshopRepo.Setup(r => r.GetByIdWithDetailsAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);
        _enrollmentRepo.Setup(r => r.HasActiveEnrollmentAsync(participant.Id, workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _enrollmentRepo.Setup(r => r.CountActiveByWorkshopIdAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var sut = CreateSut();

        await Assert.ThrowsAsync<CapacityExceededException>(() =>
            sut.EnrollAuthenticatedParticipantAsync("user-1", new CreateEnrollmentRequestDto(workshop.Id)));
    }

    [Fact]
    public async Task EnrollAuthenticatedParticipant_ReEnrollmentAfterCancellation_Succeeds()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "FOT-001", "Photography", "Basics", Guid.NewGuid(),
            FixedNowUtc.AddDays(10), FixedNowUtc.AddDays(10).AddHours(4), 15, FixedNowUtc);

        _participantRepo.Setup(r => r.GetByIdentityUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _workshopRepo.Setup(r => r.GetByIdWithDetailsAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);
        _enrollmentRepo.Setup(r => r.HasActiveEnrollmentAsync(participant.Id, workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _enrollmentRepo.Setup(r => r.CountActiveByWorkshopIdAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _enrollmentRepo.Setup(r => r.GetActiveByParticipantIdWithWorkshopAsync(participant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Enrollment>());

        var sut = CreateSut();
        var response = await sut.EnrollAuthenticatedParticipantAsync("user-1", new CreateEnrollmentRequestDto(workshop.Id));

        Assert.Equal("Active", response.Status);
        Assert.Equal("FOT-001", response.WorkshopCode);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrollAuthenticatedParticipant_OverlappingActiveWorkshop_ThrowsScheduleConflictException()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var existingWorkshop = Workshop.CreateNew(
            "COC-001", "Cooking", "Coastal recipes", Guid.NewGuid(),
            FixedNowUtc.AddDays(5).AddHours(14), FixedNowUtc.AddDays(5).AddHours(18), 12, FixedNowUtc);

        var targetWorkshop = Workshop.CreateNew(
            "DIS-001", "Design", "Canva graphics", Guid.NewGuid(),
            FixedNowUtc.AddDays(5).AddHours(16), FixedNowUtc.AddDays(5).AddHours(19), 12, FixedNowUtc);

        var activeEnrollment = Enrollment.Create(participant.Id, existingWorkshop.Id, FixedNowUtc);
        SetNavigationProperty(activeEnrollment, nameof(Enrollment.Workshop), existingWorkshop);

        _participantRepo.Setup(r => r.GetByIdentityUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _workshopRepo.Setup(r => r.GetByIdWithDetailsAsync(targetWorkshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetWorkshop);
        _enrollmentRepo.Setup(r => r.HasActiveEnrollmentAsync(participant.Id, targetWorkshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _enrollmentRepo.Setup(r => r.CountActiveByWorkshopIdAsync(targetWorkshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _enrollmentRepo.Setup(r => r.GetActiveByParticipantIdWithWorkshopAsync(participant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activeEnrollment });

        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<ScheduleConflictException>(() =>
            sut.EnrollAuthenticatedParticipantAsync("user-1", new CreateEnrollmentRequestDto(targetWorkshop.Id)));

        Assert.Equal("COC-001", ex.ConflictingWorkshopCode);
    }

    [Fact]
    public async Task EnrollAuthenticatedParticipant_BackToBackNonOverlappingWorkshop_Succeeds()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var morningWorkshop = Workshop.CreateNew(
            "FOT-001", "Morning Photo", "Light", Guid.NewGuid(),
            FixedNowUtc.AddDays(5).AddHours(8), FixedNowUtc.AddDays(5).AddHours(12), 12, FixedNowUtc);

        var afternoonWorkshop = Workshop.CreateNew(
            "COC-001", "Afternoon Cooking", "Lunch", Guid.NewGuid(),
            FixedNowUtc.AddDays(5).AddHours(12), FixedNowUtc.AddDays(5).AddHours(16), 12, FixedNowUtc);

        var activeMorning = Enrollment.Create(participant.Id, morningWorkshop.Id, FixedNowUtc);
        SetNavigationProperty(activeMorning, nameof(Enrollment.Workshop), morningWorkshop);

        _participantRepo.Setup(r => r.GetByIdentityUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _workshopRepo.Setup(r => r.GetByIdWithDetailsAsync(afternoonWorkshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(afternoonWorkshop);
        _enrollmentRepo.Setup(r => r.HasActiveEnrollmentAsync(participant.Id, afternoonWorkshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _enrollmentRepo.Setup(r => r.CountActiveByWorkshopIdAsync(afternoonWorkshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _enrollmentRepo.Setup(r => r.GetActiveByParticipantIdWithWorkshopAsync(participant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activeMorning });

        var sut = CreateSut();
        var result = await sut.EnrollAuthenticatedParticipantAsync("user-1", new CreateEnrollmentRequestDto(afternoonWorkshop.Id));

        Assert.Equal("Active", result.Status);
        Assert.Equal("COC-001", result.WorkshopCode);
    }

    [Fact]
    public async Task CancelAndCertificate_AnotherParticipantEnrollment_ThrowsForbiddenAccessException()
    {
        var owner = Participant.Create("owner-user", "Owner User", "owner@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "ORA-001", "Public Speaking", "Confidence", Guid.NewGuid(),
            FixedNowUtc.AddDays(7), FixedNowUtc.AddDays(7).AddHours(3), 20, FixedNowUtc);

        var enrollment = Enrollment.Create(owner.Id, workshop.Id, FixedNowUtc);
        SetNavigationProperty(enrollment, nameof(Enrollment.Participant), owner);
        SetNavigationProperty(enrollment, nameof(Enrollment.Workshop), workshop);

        _enrollmentRepo.Setup(r => r.GetByIdWithDetailsAsync(enrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        var sut = CreateSut();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.CancelEnrollmentAsync(enrollment.Id, "intruder-user", "intruder@correo.com", isEmployee: false));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            sut.GenerateCertificateAsync(enrollment.Id, "intruder-user", isEmployee: false));
    }

    [Fact]
    public async Task Employee_CanDownloadCertificateAndCancelAnyParticipantEnrollmentUnder24h()
    {
        var owner = Participant.Create("owner-user", "Owner User", "owner@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "ORA-001", "Public Speaking", "Confidence", Guid.NewGuid(),
            FixedNowUtc.AddHours(3), FixedNowUtc.AddHours(6), 20, FixedNowUtc);

        var enrollment = Enrollment.Create(owner.Id, workshop.Id, FixedNowUtc);
        SetNavigationProperty(enrollment, nameof(Enrollment.Participant), owner);
        SetNavigationProperty(enrollment, nameof(Enrollment.Workshop), workshop);

        _enrollmentRepo.Setup(r => r.GetByIdWithDetailsAsync(enrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        _pdfService.Setup(p => p.GenerateEnrollmentCertificate(It.IsAny<CertificateDataDto>()))
            .Returns([0x25, 0x50, 0x44, 0x46]);

        var sut = CreateSut();

        // 1. Employee downloads active certificate for another participant
        var cert = await sut.GenerateCertificateAsync(enrollment.Id, "employee-user", isEmployee: true);
        Assert.Equal("application/pdf", cert.ContentType);

        // 2. Employee cancels enrollment only 3 hours before start (no 24h restriction)
        var cancelled = await sut.CancelEnrollmentAsync(
            enrollment.Id, "employee-user", "andres.mejia@correo.com", isEmployee: true);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal("andres.mejia@correo.com", cancelled.CancelledBy);
    }

    [Fact]
    public async Task ImportEnrollments_SavesValidRowsEvenWhenOtherRowsFail_AndClearsTrackerOnError()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "FOT-001", "Photography", "Basics", Guid.NewGuid(),
            FixedNowUtc.AddDays(10), FixedNowUtc.AddDays(10).AddHours(4), capacity: 1, currentUtc: FixedNowUtc);

        var rows = new List<ExcelEnrollmentRowDto>
        {
            new(2, "", "FOT-001"),                        // Empty email
            new(3, "jorge.perez@correo.com", ""),         // Empty workshop code
            new(4, "bad-email", "FOT-001"),               // Invalid email format
            new(5, "missing@correo.com", "FOT-001"),      // Participant not found
            new(6, "jorge.perez@correo.com", "NONE-999"), // Workshop not found
            new(7, "jorge.perez@correo.com", "FOT-001"),  // Valid!
            new(8, "jorge.perez@correo.com", "FOT-001")   // Duplicate!
        };

        _excelService.Setup(s => s.ParseEnrollmentRows(It.IsAny<Stream>())).Returns(rows);
        _participantRepo.Setup(r => r.GetByEmailAsync("jorge.perez@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _workshopRepo.Setup(r => r.GetByCodeAsync("FOT-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        var savedCount = 0;
        _enrollmentRepo.Setup(r => r.HasActiveEnrollmentAsync(participant.Id, workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedCount > 0);
        _enrollmentRepo.Setup(r => r.CountActiveByWorkshopIdAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedCount);
        _enrollmentRepo.Setup(r => r.GetActiveByParticipantIdWithWorkshopAsync(participant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Enrollment>());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => savedCount++)
            .ReturnsAsync(1);

        var sut = CreateSut();
        using var stream = new MemoryStream([1, 2, 3]);
        var result = await sut.ImportEnrollmentsAsync(stream);

        Assert.Equal(1, result.EnrolledCount);
        Assert.Equal(6, result.RejectedCount);
        Assert.Equal(7, result.TotalProcessed);
        Assert.Equal([2, 3, 4, 5, 6, 8], result.RejectedRows.Select(r => r.RowNumber).ToArray());
        _unitOfWork.Verify(u => u.ClearTrackedChanges(), Times.AtLeastOnce);
    }

    private static void SetNavigationProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        var prop = typeof(TTarget).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
        prop.SetValue(target, value);
    }
}
