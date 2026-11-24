namespace Tenengroup.Infrastructure.Common.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException() : base("Entity not found")
    {
    }

    public EntityNotFoundException(string entityType, object entityId) : base($"{entityType} '{entityId}' is not found")
    {
    }
}
