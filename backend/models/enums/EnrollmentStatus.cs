namespace backend.models.enums;

public enum CourseEnrollmentStatus : short
{
    Active = 1
}

public enum ModuleEnrollmentStatus : short
{
    Enrolled = 1,
    Completed = 2,
    Withdrawn = 3,
    Failed = 4
}