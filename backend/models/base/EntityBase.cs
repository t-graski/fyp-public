namespace backend.models.@base;

public abstract class EntityBase<T>
{
   public T Id { get; init; } = default!;
}