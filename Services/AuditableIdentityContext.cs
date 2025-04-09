using AspCoreApi.Models;

namespace AspCoreApi.Services;

/// <summary>
/// Interface for auditing identity operations
/// </summary>
public interface IAuditableIdentityContext
{
    /// <summary>
    /// Logs an identity-related event
    /// </summary>
    /// <param name="eventName">The name of the event (e.g., "Login", "PasswordChanged")</param>
    /// <param name="userId">The ID of the user related to this event</param>
    /// <param name="details">Additional details about the event</param>
    Task LogIdentityEvent(string eventName, string userId, string details);
}

/// <summary>
/// Implementation of identity auditing functionality
/// </summary>
public class AuditableIdentityContext : IAuditableIdentityContext
{
    private readonly ILogger<AuditableIdentityContext> _logger;

    public AuditableIdentityContext(ILogger<AuditableIdentityContext> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs an identity-related event
    /// </summary>
    public Task LogIdentityEvent(string eventName, string userId, string details)
    {
        _logger.LogInformation("Identity Event: {EventName} for User: {UserId} - Details: {Details}",
            eventName, userId, details);

        // In a real implementation, you might also save this to a database table
        // await _dbContext.IdentityAuditLogs.AddAsync(new IdentityAuditLog 
        // {
        //     EventName = eventName,
        //     UserId = userId,
        //     Details = details,
        //     Timestamp = DateTime.UtcNow,
        //     IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        // });
        // await _dbContext.SaveChangesAsync();

        return Task.CompletedTask;
    }
}

/// <summary>
/// Interface for email sender that works with identity
/// </summary>
/// <typeparam name="TUser">The type of user</typeparam>
public interface IEmailSender<TUser> where TUser : class
{
    /// <summary>
    /// Sends an email to a user
    /// </summary>
    Task SendEmailAsync(TUser user, string subject, string body);
}

/// <summary>
/// Dummy implementation of email sender for development environments
/// </summary>
/// <typeparam name="TUser">The type of user</typeparam>
public class DummyEmailSender<TUser> : IEmailSender<TUser> where TUser : class
{
    private readonly ILogger<DummyEmailSender<ApplicationUser>> _logger;

    public DummyEmailSender(ILogger<DummyEmailSender<ApplicationUser>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the email instead of actually sending it
    /// </summary>
    public Task SendEmailAsync(TUser user, string subject, string body)
    {
        if (user is ApplicationUser appUser)
        {
            _logger.LogInformation("DUMMY EMAIL to {Email}: Subject: {Subject}, Body: {Body}",
                appUser.Email, subject, body);
        }
        else
        {
            _logger.LogInformation("DUMMY EMAIL to unknown user: Subject: {Subject}, Body: {Body}",
                subject, body);
        }

        return Task.CompletedTask;
    }
}