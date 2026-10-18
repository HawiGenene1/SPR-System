namespace SmartParkingSystem.Application.Common.Exceptions;

/// <summary>
/// Standardized JSON response contract for all errors across the API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Indicates whether the operation succeeded. Always false for error responses.
    /// </summary>
    public bool Success { get; set; } = false;

    /// <summary>
    /// Human-readable explanation of the error.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional collection of detailed validation or contextual error messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    public ErrorResponse()
    {
    }

    public ErrorResponse(string message, IEnumerable<string>? errors = null)
    {
        Success = false;
        Message = message;
        if (errors != null)
        {
            Errors.AddRange(errors);
        }
    }
}
