using backend.dtos;
using backend.models;

namespace backend.services.interfaces;

public interface IModuleService
{
    Task<ModuleDto> GetByIdAsync(Guid moduleId);

    Task<ModuleElementDto> CreateElementAsync(Guid moduleId, CreateModuleElementDto dto);
    Task<ModuleElementDto> UpdateElementAsync(Guid moduleId, Guid elementId, UpdateModuleElementDto dto);
    Task DeleteElementAsync(Guid moduleId, Guid elementId);
    Task ReorderElementsAsync(Guid moduleId, ReorderModuleElementsDto dto);

    Task<IReadOnlyList<AssessmentGradeDto>> ListAssessmentGradesAsync(Guid moduleId, Guid assessmentElementId);

    Task<AssessmentGradeDto> UpsertAssessmentGradeAsync(Guid moduleId, Guid assessmentElementId, Guid studentId, UpsertAssessmentGradeDto dto);

    Task DeleteAssessmentGradeAsync(Guid moduleId, Guid gradeId);
    Task PublishMarksAsync(Guid moduleId, bool publish);
}