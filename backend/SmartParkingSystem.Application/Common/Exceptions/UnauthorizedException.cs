namespace SmartParkingSystem.Application.Common.Exceptions;

/// <summary>
/// Thrown when authentication is missing, invalid, or expired.
/// Mapped to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "You are not authorized to access this resource.") 
        : base(message)
    {
    }
}
