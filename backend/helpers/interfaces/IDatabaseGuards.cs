namespace backend.helpers.interfaces;

public interface IDatabaseGuards
{
    Task<Guid> GetUserIdOrThrowAsync();

    Task<Guid?> TryGetStudentIdAsync(Guid userId);
    Task<Guid?> TryGetStaffIdAsync(Guid userId);

    Task<(Guid? StudentId, Guid? StaffId)> GetStudentOrStaffProfileAsync(Guid userId);

    Task EnsureCanAccessModuleAsync(Guid moduleId);
    Task EnsureTeachingStaffAsync(Guid moduleId);

    Task<Guid> EnsureTeachingStaffAndGetStaffIdAsync(Guid moduleId);
}