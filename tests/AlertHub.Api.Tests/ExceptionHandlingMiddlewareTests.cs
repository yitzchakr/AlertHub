using AlertHub.Api.Middlewares;
using AlertHub.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlertHub.Api.Tests;

/// <summary>
/// The middleware decides both what a caller learns about a failure and whether anything
/// is recorded server-side, so both halves are asserted. Driven through a
/// DefaultHttpContext rather than a running host: the behaviour under test is entirely in
/// this class, and a real pipeline would drag in the database, auth and Wolverine.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private const string Production = "Production";
    private const string Development = "Development";

    // Deliberately looks like the kind of message that made returning ex.Message a leak.
    private static readonly InvalidOperationException Unexpected = new(
        "Invalid column name 'WindowStart'. Server=prod-sql-01;Password=hunter2");

    // --- unexpected failures ---

    [Fact]
    public async Task An_unexpected_failure_returns_500()
    {
        var result = await Invoke(Unexpected, Production);

        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task An_unexpected_failure_returns_no_detail()
    {
        var result = await Invoke(Unexpected, Production);

        Assert.False(result.Body.TryGetProperty("detail", out var detail)
                     && detail.ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public async Task An_unexpected_failure_leaks_neither_schema_nor_credentials()
    {
        var result = await Invoke(Unexpected, Production);

        var body = result.Body.ToString();
        Assert.DoesNotContain("WindowStart", body);
        Assert.DoesNotContain("hunter2", body);
        Assert.DoesNotContain("prod-sql-01", body);
    }

    [Fact]
    public async Task An_unexpected_failure_returns_a_trace_id_to_quote()
    {
        var result = await Invoke(Unexpected, Production);

        Assert.True(result.Body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    // The half that justifies emptying the body. Before logging existed, that body was the
    // only record of why a request failed.
    [Fact]
    public async Task An_unexpected_failure_is_logged_with_the_exception_itself()
    {
        var result = await Invoke(Unexpected, Production);

        var logged = Assert.Single(result.Logs, l => l.Level == LogLevel.Error);
        Assert.Same(Unexpected, logged.Exception);
    }

    [Fact]
    public async Task The_log_names_the_route_and_the_trace_id()
    {
        var result = await Invoke(Unexpected, Production);

        var logged = Assert.Single(result.Logs, l => l.Level == LogLevel.Error);
        Assert.Contains("POST", logged.Message);
        Assert.Contains("/api/on-call/shifts", logged.Message);
        Assert.Contains(result.Body.GetProperty("traceId").GetString()!, logged.Message);
    }

    [Fact]
    public async Task Development_returns_the_exception_including_its_stack_trace()
    {
        var result = await Invoke(Unexpected, Development);

        var detail = result.Body.GetProperty("detail").GetString()!;
        Assert.Contains("WindowStart", detail);
        Assert.Contains(nameof(InvalidOperationException), detail);
    }

    // --- expected failures keep their author-written messages ---

    [Theory]
    [InlineData(StatusCodes.Status404NotFound, "was not found")]
    public async Task A_missing_resource_returns_404_with_its_message(int expected, string fragment)
    {
        var result = await Invoke(NotFoundException.For<string>(Guid.Empty), Production);

        Assert.Equal(expected, result.StatusCode);
        Assert.Contains(fragment, result.Body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task A_conflict_returns_409_with_its_message()
    {
        var result = await Invoke(
            new ConflictException("The window overlaps an existing shift."), Production);

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Contains("overlaps", result.Body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task A_broken_rule_returns_400_with_its_message()
    {
        var result = await Invoke(
            new DomainException("Window end must be after its start."), Production);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("after its start", result.Body.GetProperty("detail").GetString());
    }

    // Expected failures are the caller's fault, not the server's, so they must not fill the
    // error log and drown the faults that matter.
    [Theory]
    [InlineData(typeof(NotFoundException))]
    [InlineData(typeof(ConflictException))]
    [InlineData(typeof(DomainException))]
    public async Task An_expected_failure_is_not_logged_as_an_error(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "nope")!;

        var result = await Invoke(exception, Production);

        Assert.DoesNotContain(result.Logs, l => l.Level >= LogLevel.Error);
    }

    // --- validation ---

    /// <summary>
    /// The regression test this project exists for. Routing validation through a helper
    /// typed as ProblemDetails makes WriteAsJsonAsync serialize the base type and silently
    /// drop this dictionary, so every field-level message vanishes while the status code
    /// stays a plausible 400.
    /// </summary>
    [Fact]
    public async Task A_validation_failure_reports_every_field_and_message()
    {
        var result = await Invoke(
            new ValidationException(
            [
                new ValidationFailure("EndUtc", "EndUtc must be after StartUtc."),
                new ValidationFailure("ServiceId", "'Service Id' must not be empty."),
                new ValidationFailure("EndUtc", "A shift may not span more than 31 days."),
            ]),
            Production);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);

        var errors = result.Body.GetProperty("errors");
        Assert.Equal(2, errors.GetPropertyCount("EndUtc", "ServiceId"));
        Assert.Equal(2, errors.GetProperty("EndUtc").GetArrayLength());
        Assert.Contains("after StartUtc", errors.GetProperty("EndUtc")[0].GetString());
        Assert.Contains("must not be empty", errors.GetProperty("ServiceId")[0].GetString());
    }

    [Fact]
    public async Task A_validation_failure_also_carries_a_trace_id()
    {
        var result = await Invoke(
            new ValidationException([new ValidationFailure("EndUtc", "nope")]), Production);

        Assert.True(result.Body.TryGetProperty("traceId", out _));
    }

    // --- edge cases ---

    [Fact]
    public async Task A_request_that_succeeds_is_left_alone()
    {
        var logs = new List<LogEntry>();
        var context = NewContext();

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.CompletedTask, new CapturingLogger(logs), new FakeEnvironment(Production));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Empty(logs);
    }

    // Writing a second time throws over the top of the original fault, so the only honest
    // move left is to log and let the server abort the connection.
    [Fact]
    public async Task A_failure_after_the_response_started_is_logged_and_rethrown()
    {
        var logs = new List<LogEntry>();
        var context = NewContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw Unexpected, new CapturingLogger(logs), new FakeEnvironment(Production));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        Assert.Same(Unexpected, thrown);
        Assert.Contains(logs, l => l.Level == LogLevel.Error && l.Exception == Unexpected);
    }

    // --- harness ---

    private static async Task<Result> Invoke(Exception thrown, string environmentName)
    {
        var logs = new List<LogEntry>();
        var context = NewContext();
        var body = new MemoryStream();
        context.Response.Body = body;

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw thrown,
            new CapturingLogger(logs),
            new FakeEnvironment(environmentName));

        await middleware.InvokeAsync(context);

        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);

        return new Result(context.Response.StatusCode, document.RootElement.Clone(), logs);
    }

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/on-call/shifts";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed record Result(int StatusCode, JsonElement Body, List<LogEntry> Logs);

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class CapturingLogger(List<LogEntry> sink) : ILogger<ExceptionHandlingMiddleware>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            sink.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    /// <summary>
    /// DefaultHttpContext always reports HasStarted as false, so the started case needs a
    /// substitute feature to be reachable at all.
    /// </summary>
    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public string? ReasonPhrase { get; set; }
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public void OnCompleted(Func<object, Task> callback, object state) { }
        public void OnStarting(Func<object, Task> callback, object state) { }
    }
}

internal static class JsonElementExtensions
{
    /// <summary>Counts how many of the named properties are present.</summary>
    public static int GetPropertyCount(this JsonElement element, params string[] names) =>
        names.Count(name => element.TryGetProperty(name, out _));
}
