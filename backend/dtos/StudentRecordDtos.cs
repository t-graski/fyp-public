namespace backend.dtos;

public record StudentRecordDto(
    Guid Id,
    string PersonalEmail,
    string HomeAddress,
    string PhoneNumber,
    IReadOnlyList<string> EntryQualifications,
    string Gender
);

public record UpsertStudentRecordDto(
    string PersonalEmail,
    string HomeAddress,
    string PhoneNumber,
    IReadOnlyList<string> EntryQualifications,
    string Gender
);