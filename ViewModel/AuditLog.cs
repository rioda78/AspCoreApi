namespace AspCoreApi.ViewModel;



public class AuditLog
{
    public int Id { get; set; }
    public string ActionType { get; set; }
    public string PerformedBy { get; set; }
    public string TargetUserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Description { get; set; }
}

// ✅ AuditLogService
