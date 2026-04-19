namespace backend.dtos;

public record PagedDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);