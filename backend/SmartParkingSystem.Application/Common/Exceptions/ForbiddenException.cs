namespace SmartParkingSystem.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated user attempts to access or modify a resource they do not have permissions for.
/// Mapped to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to access or modify this resource.") 
        : base(message)
    {
    }
}
