using System.Reflection;
using Moq;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Services;
using SkillHub.Application.Validators;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;

namespace SkillHub.Tests.Application;

public sealed class WorkshopAndReportServiceTests
{
    private static readonly DateTime FixedNowUtc = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

    private static WorkshopService CreateWorkshopService(
        Mock<IWorkshopRepository> workshopRepo,
        Mock<IInstructorRepository> instructorRepo,
        Mock<IEnrollmentRepository>? enrollmentRepo = null,
        Mock<IUnitOfWork>? uow = null)
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(FixedNowUtc);

        return new WorkshopService(
            workshopRepo.Object,
            instructorRepo.Object,
            (enrollmentRepo ?? new Mock<IEnrollmentRepository>()).Object,
            (uow ?? new Mock<IUnitOfWork>()).Object,
            clock.Object,
            new CreateWorkshopRequestValidator());
    }

    [Fact]
    public async Task CreateWorkshop_DuplicateCode_ThrowsConflictException()
    {
        var workshopRepo = new Mock<IWorkshopRepository>();
        var instructorRepo = new Mock<IInstructorRepository>();
        workshopRepo.Setup(r => r.ExistsByCodeAsync("FOT-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateWorkshopService(workshopRepo, instructorRepo);
        var request = new CreateWorkshopRequestDto(
            "fot-001", "Photo", "Desc", "andres.mejia@correo.com",
            FixedNowUtc.AddDays(5), FixedNowUtc.AddDays(5).AddHours(3), 10);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateWorkshopAsync(request));
    }

    [Fact]
    public async Task CreateWorkshop_UnknownInstructorEmail_ThrowsNotFoundException()
    {
        var workshopRepo = new Mock<IWorkshopRepository>();
        var instructorRepo = new Mock<IInstructorRepository>();

        workshopRepo.Setup(r => r.ExistsByCodeAsync("FOT-099", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        instructorRepo.Setup(r => r.GetByEmailAsync("unknown@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instructor?)null);

        var sut = CreateWorkshopService(workshopRepo, instructorRepo);
        var request = new CreateWorkshopRequestDto(
            "FOT-099",
            "Studio Lighting",
            "Flash setups",
            "unknown@correo.com",
            FixedNowUtc.AddDays(10),
            FixedNowUtc.AddDays(10).AddHours(3),
            12);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CreateWorkshopAsync(request));
    }

    [Fact]
    public async Task CreateWorkshop_ValidInput_PersistsAndReturnsFullAvailableSpots()
    {
        var workshopRepo = new Mock<IWorkshopRepository>();
        var instructorRepo = new Mock<IInstructorRepository>();
        var uow = new Mock<IUnitOfWork>();

        var instructor = Instructor.Create("emp-1", "Andrés Mejía", "andres.mejia@correo.com", "Fotografía", FixedNowUtc);
        workshopRepo.Setup(r => r.ExistsByCodeAsync("FOT-050", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        instructorRepo.Setup(r => r.GetByEmailAsync("andres.mejia@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instructor);

        var sut = CreateWorkshopService(workshopRepo, instructorRepo, uow: uow);
        var request = new CreateWorkshopRequestDto(
            "fot-050", "Night Photo", "Long exposure", "andres.mejia@correo.com",
            FixedNowUtc.AddDays(10), FixedNowUtc.AddDays(10).AddHours(4), 16);

        var created = await sut.CreateWorkshopAsync(request);

        Assert.Equal("FOT-050", created.Code);
        Assert.Equal(16, created.Capacity);
        Assert.Equal(0, created.ActiveEnrollments);
        Assert.Equal(16, created.AvailableSpots);
        Assert.Equal("Andrés Mejía", created.InstructorName);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkshopById_And_GetWorkshopEnrollments_MissingId_ThrowsNotFoundException()
    {
        var workshopRepo = new Mock<IWorkshopRepository>();
        var instructorRepo = new Mock<IInstructorRepository>();
        var sut = CreateWorkshopService(workshopRepo, instructorRepo);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetWorkshopByIdAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetWorkshopEnrollmentsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetUpcomingWorkshops_CalculatesAvailableSpotsExcludingCancelledEnrollments()
    {
        var workshopRepo = new Mock<IWorkshopRepository>();
        var instructorRepo = new Mock<IInstructorRepository>();
        var w = Workshop.CreateNew(
            "ORA-001", "Public Speaking", "Confidence", Guid.NewGuid(),
            FixedNowUtc.AddDays(4), FixedNowUtc.AddDays(4).AddHours(3), 20, FixedNowUtc);

        var active = Enrollment.Create(Guid.NewGuid(), w.Id, FixedNowUtc);
        var cancelled = Enrollment.Create(Guid.NewGuid(), w.Id, FixedNowUtc);
        cancelled.Cancel("emp@correo.com", isEmployee: true, w.StartDateTime, FixedNowUtc);
        AddEnrollmentsToWorkshop(w, active, cancelled);

        workshopRepo.Setup(r => r.GetUpcomingWithDetailsAsync(FixedNowUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { w });

        var sut = CreateWorkshopService(workshopRepo, instructorRepo);
        var list = await sut.GetUpcomingWorkshopsAsync();

        Assert.Single(list);
        Assert.Equal(1, list[0].ActiveEnrollments);
        Assert.Equal(19, list[0].AvailableSpots);
    }

    [Fact]
    public async Task ReportService_GetTopWorkshops_OrdersDescendingByActiveEnrollmentsOnly()
    {
        var w1 = Workshop.CreateNew(
            "FOT-001", "Photo", "Desc", Guid.NewGuid(),
            FixedNowUtc.AddDays(5), FixedNowUtc.AddDays(5).AddHours(3), 10, FixedNowUtc);
        var w2 = Workshop.CreateNew(
            "COC-001", "Cooking", "Desc", Guid.NewGuid(),
            FixedNowUtc.AddDays(6), FixedNowUtc.AddDays(6).AddHours(3), 10, FixedNowUtc);

        // w1: 1 active + 2 cancelled
        var e1Active = Enrollment.Create(Guid.NewGuid(), w1.Id, FixedNowUtc);
        var e1CancelledA = Enrollment.Create(Guid.NewGuid(), w1.Id, FixedNowUtc);
        e1CancelledA.Cancel("emp@correo.com", isEmployee: true, w1.StartDateTime, FixedNowUtc);
        var e1CancelledB = Enrollment.Create(Guid.NewGuid(), w1.Id, FixedNowUtc);
        e1CancelledB.Cancel("emp@correo.com", isEmployee: true, w1.StartDateTime, FixedNowUtc);
        AddEnrollmentsToWorkshop(w1, e1Active, e1CancelledA, e1CancelledB);

        // w2: 2 active
        AddEnrollmentsToWorkshop(
            w2,
            Enrollment.Create(Guid.NewGuid(), w2.Id, FixedNowUtc),
            Enrollment.Create(Guid.NewGuid(), w2.Id, FixedNowUtc));

        var workshopRepo = new Mock<IWorkshopRepository>();
        workshopRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { w1, w2 });

        var sut = new ReportService(workshopRepo.Object);
        var report = await sut.GetTopWorkshopsAsync();

        Assert.Equal(2, report.Count);
        Assert.Equal("COC-001", report[0].Code);
        Assert.Equal(2, report[0].ActiveEnrollments);
        Assert.Equal("FOT-001", report[1].Code);
        Assert.Equal(1, report[1].ActiveEnrollments);
        Assert.Equal(9, report[1].AvailableSpots);
    }

    private static void AddEnrollmentsToWorkshop(Workshop workshop, params Enrollment[] enrollments)
    {
        var field = typeof(Workshop).GetField("_enrollments", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (List<Enrollment>)field.GetValue(workshop)!;
        list.AddRange(enrollments);
    }
}
