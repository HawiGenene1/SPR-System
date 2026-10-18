namespace SmartParkingSystem.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource or entity cannot be found.
/// Mapped to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with identifier '{key}' was not found.")
    {
    }
}
