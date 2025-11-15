using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class AuditTrail : Entity<Guid>
{
    public string EntityType { get; private set; }
    public string EntityId { get; private set; }
    public string Action { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Changes { get; private set; }
    public UserRole RoleAtTime { get; private set; }

    private AuditTrail() : base(Guid.Empty)
    {
        EntityType = null!;
        EntityId = null!;
        Action = null!;
        Changes = null!;
    }

    public static AuditTrail Create(string entityType, string entityId, string action, Guid userId, UserRole roleAtTime, string changes)
    {
        return new AuditTrail(Guid.NewGuid(), entityType, entityId, action, userId, roleAtTime, changes, DateTime.UtcNow);
    }

    private AuditTrail(Guid id, string entityType, string entityId, string action, Guid userId, UserRole roleAtTime, string changes, DateTime timestamp) : base(id)
    {
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        UserId = userId;
        RoleAtTime = roleAtTime;
        Changes = changes;
        Timestamp = timestamp;
    }
}