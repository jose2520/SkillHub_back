using System.Reflection;
using Moq;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces;
using SkillHub.Application.Services;
using SkillHub.Application.Validators;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
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
            _unitOfWork.Object,
            _clock.Object,
            new CreateEnrollmentRequestValidator());

    [Fact]
    public async Task EnrollAuthenticatedParticipant_DuplicateActive_ThrowsDuplicateEnrollmentException()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "FOT-001",
            "Photography",
            "Basics",
            Guid.NewGuid(),
            FixedNowUtc.AddDays(10),
            FixedNowUtc.AddDays(10).AddHours(4),
            15,
            FixedNowUtc);

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
    public async Task EnrollAuthenticatedParticipant_ReEnrollmentAfterCancellation_Succeeds()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "FOT-001",
            "Photography",
            "Basics",
            Guid.NewGuid(),
            FixedNowUtc.AddDays(10),
            FixedNowUtc.AddDays(10).AddHours(4),
            15,
            FixedNowUtc);

        _participantRepo.Setup(r => r.GetByIdentityUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        _workshopRepo.Setup(r => r.GetByIdWithDetailsAsync(workshop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);
        // Previous enrollment was cancelled, so HasActiveEnrollmentAsync is false
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
            "COC-001",
            "Cooking",
            "Coastal recipes",
            Guid.NewGuid(),
            FixedNowUtc.AddDays(5).AddHours(14),
            FixedNowUtc.AddDays(5).AddHours(18),
            12,
            FixedNowUtc);

        var targetWorkshop = Workshop.CreateNew(
            "DIS-001",
            "Design",
            "Canva graphics",
            Guid.NewGuid(),
            FixedNowUtc.AddDays(5).AddHours(16),
            FixedNowUtc.AddDays(5).AddHours(19),
            12,
            FixedNowUtc);

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
    public async Task CancelAndCertificate_AnotherParticipantEnrollment_ThrowsForbiddenAccessException()
    {
        var owner = Participant.Create("owner-user", "Owner User", "owner@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "ORA-001",
            "Public Speaking",
            "Confidence",
            Guid.NewGuid(),
            FixedNowUtc.AddDays(7),
            FixedNowUtc.AddDays(7).AddHours(3),
            20,
            FixedNowUtc);

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
    public async Task ImportEnrollments_SavesValidRowsEvenWhenOtherRowsFail()
    {
        var participant = Participant.Create("user-1", "Jorge Perez", "jorge.perez@correo.com", FixedNowUtc);
        var workshop = Workshop.CreateNew(
            "FOT-001",
            "Photography",
            "Basics",
            Guid.NewGuid(),
            FixedNowUtc.AddDays(10),
            FixedNowUtc.AddDays(10).AddHours(4),
            capacity: 1,
            currentUtc: FixedNowUtc);

        var rows = new List<ExcelEnrollmentRowDto>
        {
            new(2, "missing@correo.com", "FOT-001"),
            new(3, "jorge.perez@correo.com", "UNKNOWN-999"),
            new(4, "jorge.perez@correo.com", "FOT-001"), // Valid!
            new(5, "jorge.perez@correo.com", "FOT-001")  // Duplicate!
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
        Assert.Equal(3, result.RejectedCount);
        Assert.Equal(4, result.TotalProcessed);
        Assert.Equal([2, 3, 5], result.RejectedRows.Select(r => r.RowNumber).ToArray());
    }

    private static void SetNavigationProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        var prop = typeof(TTarget).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
        prop.SetValue(target, value);
    }
}
