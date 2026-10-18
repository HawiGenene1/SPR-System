namespace SmartParkingSystem.Application.Common.Exceptions;

/// <summary>
/// Thrown when a database or persistence layer operation fails.
/// Mapped to HTTP 500 Internal Server Error (masking sensitive internal schema details).
/// </summary>
public class DatabaseException : Exception
{
    public DatabaseException(string message, Exception? innerException = null) 
        : base(message, innerException)
    {
    }
}
