using AspCoreApi.Data;
using AspCoreApi.ViewModel;

namespace AspCoreApi.Services;


public interface IAuditLogService
{
    Task LogAsync(string actionType, string performedBy, string targetUserId, string description);
}

public class AuditLogServis : IAuditLogService
{
    
    private readonly ApplicationDbContext _context;

    public AuditLogServis(ApplicationDbContext context)
    {
        _context = context;
    }
    
    
    public async Task LogAsync(string actionType, string performedBy, string targetUserId, string description)
    {
        var log = new AuditLog
        {
            ActionType = actionType,
            PerformedBy = performedBy,
            TargetUserId = targetUserId,
            Description = description,
            Timestamp = DateTime.UtcNow
        };
        _context.Set<AuditLog>().Add(log);
        await _context.SaveChangesAsync();
    }
}