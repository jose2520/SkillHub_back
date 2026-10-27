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

    [Fact]
    public async Task CreateWorkshop_UnknownInstructorEmail_ThrowsNotFoundException()
    {
        var workshopRepo = new Mock<IWorkshopRepository>();
        var instructorRepo = new Mock<IInstructorRepository>();
        var enrollmentRepo = new Mock<IEnrollmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(FixedNowUtc);

        workshopRepo.Setup(r => r.ExistsByCodeAsync("FOT-099", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        instructorRepo.Setup(r => r.GetByEmailAsync("unknown@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instructor?)null);

        var sut = new WorkshopService(
            workshopRepo.Object,
            instructorRepo.Object,
            enrollmentRepo.Object,
            uow.Object,
            clock.Object,
            new CreateWorkshopRequestValidator());

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
