using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/modules")]
public class ModuleController(IModuleService modules) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ModuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id) => Ok(ApiResponse<ModuleDto>.Ok(await modules.GetByIdAsync(id)));

    [HttpPost("{moduleId:guid}/elements")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ModuleElementDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateElement(Guid moduleId, CreateModuleElementDto dto)
    {
        var created = await modules.CreateElementAsync(moduleId, dto);
        return StatusCode(201, ApiResponse<ModuleElementDto>.Ok(created, 201));
    }

    [HttpPut("{moduleId:guid}/elements/{elementId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ModuleElementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateElement(Guid moduleId, Guid elementId, UpdateModuleElementDto dto)
        => Ok(ApiResponse<ModuleElementDto>.Ok(await modules.UpdateElementAsync(moduleId, elementId, dto)));

    [HttpDelete("{moduleId:guid}/elements/{elementId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteElement(Guid moduleId, Guid elementId)
    {
        await modules.DeleteElementAsync(moduleId, elementId);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPut("{moduleId:guid}/elements/reorder")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reorder(Guid moduleId, ReorderModuleElementsDto dto)
    {
        await modules.ReorderElementsAsync(moduleId, dto);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("{moduleId:guid}/assessments/{assessmentElementId:guid}/grades")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AssessmentGradeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListGrades(Guid moduleId, Guid assessmentElementId)
        => Ok(ApiResponse<IReadOnlyList<AssessmentGradeDto>>.Ok(
            await modules.ListAssessmentGradesAsync(moduleId, assessmentElementId)
        ));

    [HttpPut("{moduleId:guid}/assessments/{assessmentElementId:guid}/grades/{studentId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AssessmentGradeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertGrade(Guid moduleId, Guid assessmentElementId, Guid studentId,
        UpsertAssessmentGradeDto dto)
        => Ok(ApiResponse<AssessmentGradeDto>.Ok(
            await modules.UpsertAssessmentGradeAsync(moduleId, assessmentElementId, studentId, dto)
        ));

    [HttpDelete("{moduleId:guid}/grades/{gradeId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGrade(Guid moduleId, Guid gradeId)
    {
        await modules.DeleteAssessmentGradeAsync(moduleId, gradeId);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("{moduleId:guid}/publish-marks")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishMarks(Guid moduleId, [FromQuery] bool publish = true)
    {
        await modules.PublishMarksAsync(moduleId, publish);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}