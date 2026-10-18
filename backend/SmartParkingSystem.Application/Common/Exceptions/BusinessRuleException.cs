namespace SmartParkingSystem.Application.Common.Exceptions;

/// <summary>
/// Thrown when a domain or business rule is violated.
/// Mapped to HTTP 400 Bad Request.
/// </summary>
public class BusinessRuleException : Exception
{
    public List<string> Errors { get; } = new();

    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors.AddRange(errors);
    }
}
