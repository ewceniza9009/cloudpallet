using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security;
using WMS.Domain.Aggregates.DockAppointment;
using WMS.Domain.Aggregates.Warehouse;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Shared;

namespace WMS.Infrastructure.Persistence;

public class WmsDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    private readonly IPublisher _publisher;
    private readonly ICurrentUserService _currentUserService;

    public WmsDbContext(
        DbContextOptions<WmsDbContext> options,
        IPublisher publisher,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _publisher = publisher;
        _currentUserService = currentUserService;
    }

    // Core & Foundational Setup
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<PalletType> PalletTypes => Set<PalletType>();
    public DbSet<Rate> Rates => Set<Rate>();

    // Warehouse Structure & Master Data
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Dock> Docks => Set<Dock>();
    public DbSet<YardSpot> YardSpots => Set<YardSpot>();
    public DbSet<Location> Locations => Set<Location>();

    // Product & Inventory Master Data
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<BillOfMaterial> BillOfMaterials => Set<BillOfMaterial>();
    public DbSet<BillOfMaterialLine> BillOfMaterialLines => Set<BillOfMaterialLine>();

    // Core Operational Aggregates & Entities
    public DbSet<DockAppointment> DockAppointments => Set<DockAppointment>();
    public DbSet<Receiving> Receivings => Set<Receiving>();
    public DbSet<Pallet> Pallets => Set<Pallet>();
    public DbSet<PalletLine> PalletLines => Set<PalletLine>();
    public DbSet<MaterialInventory> MaterialInventories => Set<MaterialInventory>();
    public DbSet<ItemTransferTransaction> ItemTransferTransactions => Set<ItemTransferTransaction>();

    // Transaction Logs
    public DbSet<PickTransaction> PickTransactions => Set<PickTransaction>();
    public DbSet<PutawayTransaction> PutawayTransactions => Set<PutawayTransaction>();
    public DbSet<WithdrawalTransaction> WithdrawalTransactions => Set<WithdrawalTransaction>();
    public DbSet<TransferTransaction> TransferTransactions => Set<TransferTransaction>();
    public DbSet<VASTransaction> VASTransactions => Set<VASTransaction>();
    public DbSet<VASTransactionLine> VASTransactionLines => Set<VASTransactionLine>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>(); 

    // Financial & Billing Outputs
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    // System & Auditing
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(utcConverter);
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();

        var domainEvents = ChangeTracker.Entries<User>().AsEnumerable<EntityEntry>()
            .Concat(ChangeTracker.Entries<AggregateRoot<Guid>>())
            .Select(e => e.Entity)
            .SelectMany(e =>
            {
                if (e is User user && user.DomainEvents.Any())
                {
                    var events = user.DomainEvents.ToList();
                    user.ClearDomainEvents();
                    return events;
                }
                if (e is AggregateRoot<Guid> root && root.DomainEvents.Any())
                {
                    var events = root.DomainEvents.ToList();
                    root.ClearDomainEvents();
                    return events;
                }
                return new List<IDomainEvent>();
            }).ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        return result;
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<AuditableEntity<Guid>>();
        var userId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(e => e.CreatedBy).CurrentValue = userId;
                entry.Property(e => e.CreatedOn).CurrentValue = now;
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Property(e => e.LastModifiedBy).CurrentValue = userId;
                entry.Property(e => e.LastModifiedOn).CurrentValue = now;
            }
        }
    }
}