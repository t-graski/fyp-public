namespace backend.dtos;

public record AdminStudentRecordDto(
    Guid Id,
    string PersonalEmail,
    string HomeAddress,
    string PhoneNumber,
    IReadOnlyList<string> EntryQualifications,
    string Gender
);

public record AdminUpsertStudentRecordDto(
    string PersonalEmail,
    string HomeAddress,
    string PhoneNumber,
    IReadOnlyList<string> EntryQualifications,
    string Gender
);