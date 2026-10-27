namespace SkillHub.Domain.Errors;

public static class DomainErrors
{
    public static class Workshop
    {
        public static DomainError NotFound(string identifier) =>
            DomainError.NotFound("Workshop.NotFound", $"Workshop '{identifier}' was not found.");

        public static DomainError DuplicateCode(string code) =>
            DomainError.Conflict("Workshop.DuplicateCode", $"A workshop with code '{code}' already exists.");

        public static DomainError PastStartDate =>
            DomainError.Validation("Workshop.PastStartDate", "A workshop must always be created with a future start date and time.");

        public static DomainError InvalidSchedule =>
            DomainError.Validation("Workshop.InvalidSchedule", "The workshop end date and time must be strictly after the start date and time.");

        public static DomainError InvalidCapacity =>
            DomainError.Validation("Workshop.InvalidCapacity", "Workshop maximum capacity must be greater than zero.");

        public static DomainError AlreadyStarted(string code) =>
            DomainError.Conflict("Workshop.AlreadyStarted", $"Cannot enroll in workshop '{code}' because it has already started.");

        public static DomainError CapacityExceeded(string code, int capacity) =>
            DomainError.Conflict("Workshop.CapacityExceeded", $"Workshop '{code}' has reached its maximum capacity of {capacity} active participants.");
    }

    public static class Enrollment
    {
        public static DomainError NotFound(Guid id) =>
            DomainError.NotFound("Enrollment.NotFound", $"Enrollment with ID '{id}' was not found.");

        public static DomainError DuplicateActive(string workshopCode) =>
            DomainError.Conflict("Enrollment.DuplicateActive", $"Participant already has an active enrollment in workshop '{workshopCode}'.");

        public static DomainError ScheduleConflict(string conflictingCode, DateTime startUtc, DateTime endUtc) =>
            DomainError.Conflict(
                "Enrollment.ScheduleConflict",
                $"Schedule conflict detected with active enrollment in workshop '{conflictingCode}' ({startUtc:yyyy-MM-dd HH:mm} - {endUtc:HH:mm} UTC).");

        public static DomainError AlreadyCancelled =>
            DomainError.Conflict("Enrollment.AlreadyCancelled", "This enrollment is already cancelled.");

        public static DomainError WorkshopAlreadyStarted =>
            DomainError.Conflict("Enrollment.WorkshopAlreadyStarted", "Cannot cancel an enrollment after the workshop has already started.");

        public static DomainError CancellationWindowClosed =>
            DomainError.Conflict("Enrollment.CancellationWindowClosed", "Participants can only cancel an enrollment at least 24 hours before the workshop starts.");

        public static DomainError CertificateRequiresActive =>
            DomainError.Conflict("Enrollment.CertificateRequiresActive", "Enrollment certificates can only be generated for active enrollments.");
    }

    public static class Instructor
    {
        public static DomainError NotFoundByEmail(string email) =>
            DomainError.NotFound("Instructor.NotFound", $"No registered instructor was found with email '{email}'.");
    }

    public static class Participant
    {
        public static DomainError NotFoundByEmail(string email) =>
            DomainError.NotFound("Participant.NotFound", $"No registered participant was found with email '{email}'.");
    }
}
