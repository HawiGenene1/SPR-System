using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartParkingSystem.Api.Middleware;
using SmartParkingSystem.Application.Common.Exceptions;

namespace SmartParkingSystem.UnitTests;

public static class ExceptionMiddlewareBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running ExceptionMiddleware & Error Handling Unit Tests");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        // 1. Validation Errors (400 Bad Request)
        TestValidationExceptionWithErrorsList();
        TestArgumentExceptionHandling();
        TestArgumentNullExceptionHandling();

        // 2. Unauthorized Errors (401 Unauthorized)
        TestUnauthorizedExceptionHandling();
        TestUnauthorizedAccessExceptionForAuth();

        // 3. Forbidden Errors (403 Forbidden)
        TestForbiddenExceptionHandling();
        TestUnauthorizedAccessExceptionForForbiddenAccess();

        // 4. Not Found Errors (404 Not Found)
        TestNotFoundExceptionHandling();
        TestKeyNotFoundExceptionHandling();

        // 5. Business Rule Exceptions (400 Bad Request)
        TestBusinessRuleExceptionHandling();
        TestInvalidOperationExceptionHandling();

        // 6. Database Exceptions (409 Conflict & 500 Internal Server Error)
        TestDbUpdateConcurrencyExceptionHandling();
        TestDbUpdateUniqueConstraintExceptionHandling();
        TestGenericDbUpdateExceptionMasking();
        TestCustomDatabaseExceptionMasking();

        // 7. Unexpected Exceptions (500 Internal Server Error)
        TestUnexpectedExceptionHandlingInProduction();

        // 8. Information Leak & Stack Trace Suppression
        TestStackTraceNotExposedInResponseBody();
        TestConsistentJsonEnvelopeStructure();

        // 9. Structured Logging Verification
        TestStructuredLoggingForClientWarnings();
        TestStructuredLoggingForServerErrors();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Exception Handling Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Exception Handling Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        return _failed == 0 ? 0 : 1;
    }

    private static void TestValidationExceptionWithErrorsList()
    {
        var validationErrors = new List<string>
        {
            "Reservation start time cannot be in the past.",
            "Vehicle ID is required."
        };
        var ex = new ValidationException("Validation failed for reservation request.", validationErrors);

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 400, "ValidationException returns HTTP 400 Bad Request");
        Assert(body.Success == false, "Validation response has success = false");
        Assert(body.Message == "Validation failed for reservation request.", "Validation response retains validation message");
        Assert(body.Errors.Count == 2, "Validation response contains 2 detailed error messages");
        Assert(body.Errors[0] == validationErrors[0], "Validation error [0] matches expected message");
        Assert(body.Errors[1] == validationErrors[1], "Validation error [1] matches expected message");
    }

    private static void TestArgumentExceptionHandling()
    {
        var ex = new ArgumentException("Parking slot code cannot be null or whitespace.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 400, "ArgumentException returns HTTP 400 Bad Request");
        Assert(body.Success == false, "ArgumentException response has success = false");
        Assert(body.Message == "Parking slot code cannot be null or whitespace.", "ArgumentException response retains parameter message");
        Assert(body.Errors != null && body.Errors.Count == 0, "ArgumentException response returns empty errors list");
    }

    private static void TestArgumentNullExceptionHandling()
    {
        var ex = new ArgumentNullException("request", "Request body cannot be null.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 400, "ArgumentNullException returns HTTP 400 Bad Request");
        Assert(body.Success == false, "ArgumentNullException response has success = false");
        Assert(body.Message.Contains("Request body cannot be null"), "ArgumentNullException response retains explanation");
        Assert(body.Errors != null && body.Errors.Count == 0, "ArgumentNullException response returns empty errors list");
    }

    private static void TestUnauthorizedExceptionHandling()
    {
        var ex = new UnauthorizedException("Invalid credentials or authentication token expired.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 401, "UnauthorizedException returns HTTP 401 Unauthorized");
        Assert(body.Success == false, "Unauthorized response has success = false");
        Assert(body.Message == "Invalid credentials or authentication token expired.", "Unauthorized response has correct message");
        Assert(body.Errors.Count == 0, "Unauthorized response errors array is empty");
    }

    private static void TestUnauthorizedAccessExceptionForAuth()
    {
        var ex = new UnauthorizedAccessException("Invalid email or password.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 401, "UnauthorizedAccessException with login failure returns HTTP 401 Unauthorized");
        Assert(body.Success == false, "UnauthorizedAccessException response has success = false");
        Assert(body.Message == "Invalid email or password.", "UnauthorizedAccessException response message matches");
        Assert(body.Errors.Count == 0, "UnauthorizedAccessException response errors array is empty");
    }

    private static void TestForbiddenExceptionHandling()
    {
        var ex = new ForbiddenException("You do not have permission to view this customer's parking history.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 403, "ForbiddenException returns HTTP 403 Forbidden");
        Assert(body.Success == false, "Forbidden response has success = false");
        Assert(body.Message == "You do not have permission to view this customer's parking history.", "Forbidden response has correct message");
        Assert(body.Errors.Count == 0, "Forbidden response errors array is empty");
    }

    private static void TestUnauthorizedAccessExceptionForForbiddenAccess()
    {
        var ex = new UnauthorizedAccessException("Access denied: You can only modify your own vehicles.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 403, "UnauthorizedAccessException with 'Access denied' returns HTTP 403 Forbidden");
        Assert(body.Success == false, "Forbidden access response has success = false");
        Assert(body.Message == "Access denied: You can only modify your own vehicles.", "Forbidden access response message matches");
        Assert(body.Errors.Count == 0, "Forbidden access response errors array is empty");
    }

    private static void TestNotFoundExceptionHandling()
    {
        var slotId = Guid.NewGuid();
        var ex = new NotFoundException("ParkingSlot", slotId);

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 404, "NotFoundException returns HTTP 404 Not Found");
        Assert(body.Success == false, "NotFound response has success = false");
        Assert(body.Message.Contains(slotId.ToString()), "NotFound response contains entity identifier");
        Assert(body.Errors.Count == 0, "NotFound response errors array is empty");
    }

    private static void TestKeyNotFoundExceptionHandling()
    {
        var ex = new KeyNotFoundException("Reservation was not found or has been purged.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 404, "KeyNotFoundException returns HTTP 404 Not Found");
        Assert(body.Success == false, "KeyNotFound response has success = false");
        Assert(body.Message == "Reservation was not found or has been purged.", "KeyNotFound response has correct message");
        Assert(body.Errors.Count == 0, "KeyNotFound response errors array is empty");
    }

    private static void TestBusinessRuleExceptionHandling()
    {
        var ex = new BusinessRuleException("Parking slot is already reserved.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 400, "BusinessRuleException returns HTTP 400 Bad Request");
        Assert(body.Success == false, "BusinessRule response has success = false");
        Assert(body.Message == "Parking slot is already reserved.", "BusinessRule response message matches user prompt example");
        Assert(body.Errors.Count == 0, "BusinessRule response errors array is empty");
    }

    private static void TestInvalidOperationExceptionHandling()
    {
        var ex = new InvalidOperationException("The selected parking slot is already reserved for the requested time frame.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 400, "InvalidOperationException returns HTTP 400 Bad Request");
        Assert(body.Success == false, "InvalidOperationException response has success = false");
        Assert(body.Message == "The selected parking slot is already reserved for the requested time frame.", "InvalidOperationException message matches");
        Assert(body.Errors.Count == 0, "InvalidOperationException errors array is empty");
    }

    private static void TestDbUpdateConcurrencyExceptionHandling()
    {
        var ex = new DbUpdateConcurrencyException("Concurrency conflict occurred on ParkingSlot entity.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 409, "DbUpdateConcurrencyException returns HTTP 409 Conflict");
        Assert(body.Success == false, "Concurrency conflict response has success = false");
        Assert(body.Message.Contains("concurrency conflict", StringComparison.OrdinalIgnoreCase), "Concurrency conflict message explains retry instruction");
        Assert(body.Errors.Count == 0, "Concurrency conflict errors array is empty");
    }

    private static void TestDbUpdateUniqueConstraintExceptionHandling()
    {
        var innerEx = new Exception("Postgres error 23505: duplicate key value violates unique constraint 'IX_Vehicles_LicensePlate'");
        var ex = new DbUpdateException("An error occurred while saving the entity changes.", innerEx);

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 409, "DbUpdateException with unique constraint violation returns HTTP 409 Conflict");
        Assert(body.Success == false, "Unique constraint violation response has success = false");
        Assert(body.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase), "Unique constraint violation message explains duplicate record");
        Assert(!body.Message.Contains("IX_Vehicles_LicensePlate"), "Database internal constraint name is not leaked in message");
        Assert(body.Errors.Count == 0, "Unique constraint violation errors array is empty");
    }

    private static void TestGenericDbUpdateExceptionMasking()
    {
        var innerEx = new Exception("SELECT * FROM \"Users\" WHERE \"PasswordHash\" = 'secret'");
        var ex = new DbUpdateException("Low-level database connection terminated unexpectedly.", innerEx);

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 500, "General DbUpdateException returns HTTP 500 Internal Server Error");
        Assert(body.Success == false, "Database error response has success = false");
        Assert(body.Message == "A database error occurred while processing your request.", "Database error response message is securely masked");
        Assert(!body.Message.Contains("Users") && !body.Message.Contains("PasswordHash"), "Database SQL queries and table names are NEVER leaked");
        Assert(body.Errors.Count == 0, "Database error errors array is empty");
    }

    private static void TestCustomDatabaseExceptionMasking()
    {
        var ex = new DatabaseException("Failed to execute stored procedure on production primary cluster.");

        var (statusCode, body, _) = ExecuteMiddleware(ex);

        Assert(statusCode == 500, "DatabaseException returns HTTP 500 Internal Server Error");
        Assert(body.Success == false, "DatabaseException response has success = false");
        Assert(body.Message == "A database error occurred while processing your request.", "DatabaseException message is safely masked");
        Assert(!body.Message.Contains("primary cluster"), "Internal cluster topology details are not exposed");
        Assert(body.Errors.Count == 0, "DatabaseException errors array is empty");
    }

    private static void TestUnexpectedExceptionHandlingInProduction()
    {
        var ex = new NullReferenceException("Object reference not set to an instance of an object at Controller.Action line 42");

        var (statusCode, body, _) = ExecuteMiddleware(ex, isDevelopment: false);

        Assert(statusCode == 500, "Unhandled exception in production returns HTTP 500 Internal Server Error");
        Assert(body.Success == false, "Production unexpected error response has success = false");
        Assert(body.Message == "An unexpected error occurred. Please try again later.", "Production response uses user-friendly generic message");
        Assert(!body.Message.Contains("NullReferenceException"), "Production response does not expose exception class name");
        Assert(!body.Message.Contains("line 42"), "Production response does not expose source code line numbers");
        Assert(body.Errors.Count == 0, "Production response errors array is empty");
    }

    private static void TestStackTraceNotExposedInResponseBody()
    {
        try
        {
            throw new InvalidOperationException("Simulated failure to generate full call stack trace.");
        }
        catch (Exception ex)
        {
            var (statusCode, _, rawJson) = ExecuteMiddlewareRaw(ex, isDevelopment: false);

            Assert(statusCode == 400, "Status code is 400 for InvalidOperationException");
            Assert(!rawJson.Contains("stackTrace", StringComparison.OrdinalIgnoreCase), "Raw JSON does not contain 'stackTrace'");
            Assert(!rawJson.Contains("at SmartParkingSystem", StringComparison.OrdinalIgnoreCase), "Raw JSON does not contain stack frames");
            Assert(!rawJson.Contains("ExecuteMiddlewareRaw", StringComparison.OrdinalIgnoreCase), "Raw JSON does not contain internal function names");
        }
    }

    private static void TestConsistentJsonEnvelopeStructure()
    {
        var ex = new BusinessRuleException("Parking slot is already reserved.");

        var (statusCode, _, rawJson) = ExecuteMiddlewareRaw(ex);

        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        Assert(root.TryGetProperty("success", out var successProp) && successProp.ValueKind == JsonValueKind.False, "JSON has property 'success': false");
        Assert(root.TryGetProperty("message", out var msgProp) && msgProp.GetString() == "Parking slot is already reserved.", "JSON has property 'message': 'Parking slot is already reserved.'");
        Assert(root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array && errorsProp.GetArrayLength() == 0, "JSON has property 'errors': []");
    }

    private static void TestStructuredLoggingForClientWarnings()
    {
        var logger = new TestLogger<ExceptionMiddleware>();
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var middleware = new ExceptionMiddleware(
            _ => throw new BusinessRuleException("Parking slot is already reserved."),
            logger,
            env);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/reservations";
        context.Response.Body = new MemoryStream();

        middleware.InvokeAsync(context).GetAwaiter().GetResult();

        Assert(logger.WarningLogs.Count >= 1, "Client 4xx error is logged as Warning");
        var warning = logger.WarningLogs[0];
        Assert(warning.Contains("POST"), "Warning log contains request method 'POST'");
        Assert(warning.Contains("/api/reservations"), "Warning log contains request path '/api/reservations'");
        Assert(warning.Contains("400"), "Warning log contains status code 400");
        Assert(warning.Contains("BusinessRuleException"), "Warning log contains exception type 'BusinessRuleException'");
    }

    private static void TestStructuredLoggingForServerErrors()
    {
        var logger = new TestLogger<ExceptionMiddleware>();
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var middleware = new ExceptionMiddleware(
            _ => throw new DatabaseException("Database server connection dropped."),
            logger,
            env);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/reports/revenue";
        context.Response.Body = new MemoryStream();

        middleware.InvokeAsync(context).GetAwaiter().GetResult();

        Assert(logger.ErrorLogs.Count >= 1, "Server 5xx error is logged as Error");
        var error = logger.ErrorLogs[0];
        Assert(error.Contains("GET"), "Error log contains request method 'GET'");
        Assert(error.Contains("/api/reports/revenue"), "Error log contains request path '/api/reports/revenue'");
        Assert(error.Contains("500"), "Error log contains status code 500");
        Assert(error.Contains("DatabaseException"), "Error log contains exception type 'DatabaseException'");
    }

    private static (int StatusCode, ErrorResponse Body, string RawJson) ExecuteMiddleware(Exception ex, bool isDevelopment = false)
    {
        var (statusCode, body, rawJson) = ExecuteMiddlewareRaw(ex, isDevelopment);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserialized = JsonSerializer.Deserialize<ErrorResponse>(rawJson, options)!;
        return (statusCode, deserialized, rawJson);
    }

    private static (int StatusCode, ErrorResponse? Body, string RawJson) ExecuteMiddlewareRaw(Exception ex, bool isDevelopment = false)
    {
        var logger = new TestLogger<ExceptionMiddleware>();
        var env = new TestHostEnvironment
        {
            EnvironmentName = isDevelopment ? Environments.Development : Environments.Production
        };

        var middleware = new ExceptionMiddleware(
            _ => throw ex,
            logger,
            env);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/reservations";
        var memStream = new MemoryStream();
        context.Response.Body = memStream;

        middleware.InvokeAsync(context).GetAwaiter().GetResult();

        memStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(memStream);
        var rawJson = reader.ReadToEnd();

        return (context.Response.StatusCode, null, rawJson);
    }

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            _passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] {message}");
            Console.ResetColor();
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {message}");
            Console.ResetColor();
        }
    }

    private class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "SmartParkingSystem.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private class TestLogger<T> : ILogger<T>
    {
        public List<string> InfoLogs { get; } = new();
        public List<string> WarningLogs { get; } = new();
        public List<string> ErrorLogs { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (exception != null)
            {
                message += $" | Exception: {exception.GetType().Name}: {exception.Message}";
            }

            switch (logLevel)
            {
                case LogLevel.Information:
                    InfoLogs.Add(message);
                    break;
                case LogLevel.Warning:
                    WarningLogs.Add(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    ErrorLogs.Add(message);
                    break;
            }
        }
    }
}
