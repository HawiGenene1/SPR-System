namespace SmartParkingSystem.Application.Common.Exceptions;

/// <summary>
/// Thrown when one or more input validation rules fail.
/// Mapped to HTTP 400 Bad Request.
/// </summary>
public class ValidationException : Exception
{
    public List<string> Errors { get; } = new();

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors.AddRange(errors);
    }

    public ValidationException(IEnumerable<string> errors) 
        : base("One or more validation failures have occurred.")
    {
        Errors.AddRange(errors);
    }
}
