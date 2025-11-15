using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using WMS.Application.Abstractions.Security;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Domain.Shared; // ADD THIS USING DIRECTIVE

namespace WMS.Infrastructure.Persistence.Interceptors;

public class AuditingInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditTrails(eventData.Context!);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditTrails(eventData.Context!);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditTrails(DbContext context)
    {
        context.ChangeTracker.DetectChanges();
        var auditTrails = new List<AuditTrail>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // THIS IS THE CORRECTED LINE: It now also checks if the entity is a proper Entity<Guid>
            if (entry.Entity is not Entity<Guid> || entry.Entity is AuditTrail || entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            var userId = currentUserService.UserId ?? Guid.Empty;
            var userRole = currentUserService.UserRole ?? UserRole.Admin;

            var changes = GetChanges(entry);
            if (changes is null) continue;

            var audit = AuditTrail.Create(
                entry.Entity.GetType().Name,
                entry.Property("Id").CurrentValue!.ToString()!,
                entry.State.ToString(),
                userId,
                userRole,
                changes);

            auditTrails.Add(audit);
        }

        if (auditTrails.Any())
        {
            context.Set<AuditTrail>().AddRange(auditTrails);
        }
    }

    private string? GetChanges(EntityEntry entry)
    {
        if (entry.State == EntityState.Added) return JsonSerializer.Serialize(entry.CurrentValues.ToObject());
        if (entry.State == EntityState.Deleted) return JsonSerializer.Serialize(entry.OriginalValues.ToObject());
        if (entry.State == EntityState.Modified)
        {
            var changes = new Dictionary<string, object>();
            foreach (var prop in entry.OriginalValues.Properties)
            {
                var originalValue = entry.OriginalValues[prop];
                var currentValue = entry.CurrentValues[prop];
                if (!Equals(originalValue, currentValue))
                {
                    changes[prop.Name] = new { Original = originalValue, Current = currentValue };
                }
            }
            return changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
        }
        return null;
    }
}