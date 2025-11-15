using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;
using WMS.Application.Features.Reports.Queries;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Infrastructure.Persistence.Repositories;

public class ReportRepository(WmsDbContext context, IClock clock) : IReportRepository   
{
    private static readonly Dictionary<ServiceType, string> StorageTierMap = new()
    {
        { ServiceType.FrozenStorage, "FrozenStorage" },
        { ServiceType.Chilling, "Chilling" },
        { ServiceType.Staging, "Staging" },
        { ServiceType.CoolStorage, "CoolStorage" },
        { ServiceType.DeepFrozenStorage, "DeepFrozen" },
        { ServiceType.ULTStorage, "ULT" }
    };

    private class LedgerEntryIntermediate
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public Guid MaterialId { get; set; }
        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal WeightIn { get; set; }
        public decimal WeightOut { get; set; }
        public Guid AccountId { get; set; }
        public Guid? SupplierId { get; set; }
        public Guid? TruckId { get; set; }
        public Guid? UserId { get; set; }
    }

    private class RawLogData
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public Guid? AccountId { get; set; }
        public string? Arg1 { get; set; }
        public string? Arg2 { get; set; }
        public string? Arg3 { get; set; }
        public decimal? Value1 { get; set; }
        public string? Description { get; set; }
        public Guid? TruckId { get; set; }
    }

    public async Task<Application.Common.Models.PagedResult<InventoryLedgerGroupDto>> GetInventoryLedgerAsync(GetInventoryLedgerQuery filter, CancellationToken cancellationToken)
    {
        var receivingQuery = context.PalletLines.AsNoTracking()
            .Select(pl => new LedgerEntryIntermediate
            {
                Date = pl.Pallet.Receiving.Timestamp,
                Type = "Receiving",
                Document = "RECV-" + pl.Pallet.Receiving.Id.ToString().Substring(0, 8).ToUpper(),
                MaterialName = pl.Material.Name,
                MaterialId = pl.MaterialId,
                QuantityIn = pl.Quantity,
                QuantityOut = 0m,
                WeightIn = pl.Weight,
                WeightOut = 0m,
                AccountId = pl.AccountId,
                SupplierId = (Guid?)pl.Pallet.Receiving.SupplierId,
                TruckId = pl.Pallet.Receiving.Appointment.TruckId,
                UserId = pl.CreatedBy
            });

        var pickingQuery = context.PickTransactions.AsNoTracking()
            .Select(pt => new LedgerEntryIntermediate
            {
                Date = pt.Timestamp,
                Type = "Picking",
                Document = pt.WithdrawalTransactions.FirstOrDefault() != null ? pt.WithdrawalTransactions.First().ShipmentNumber : "PICK-" + pt.Id.ToString().Substring(0, 8).ToUpper(),
                MaterialName = pt.MaterialInventory.Material.Name,
                MaterialId = pt.MaterialInventory.MaterialId,
                QuantityIn = 0m,
                QuantityOut = pt.Quantity,
                WeightIn = 0m,
                WeightOut = pt.PickWeight,
                AccountId = pt.AccountId,
                SupplierId = (Guid?)null,
                TruckId = pt.WithdrawalTransactions.FirstOrDefault() != null ? pt.WithdrawalTransactions.First().Appointment!.TruckId : null,
                UserId = pt.UserId
            });

        var vasInputQuery = from vt in context.VASTransactions.AsNoTracking()
                      .Where(vt =>
                        vt.ServiceType == ServiceType.Repack ||
                        vt.ServiceType == ServiceType.Split ||
                        vt.ServiceType == ServiceType.Kitting)
                            join vl in context.VASTransactionLines on vt.Id equals vl.VASTransactionId         
                            join m in context.Materials on vl.MaterialId.Value equals m.Id
                            where vl.IsInput && vl.MaterialId.HasValue
                            select new LedgerEntryIntermediate
                            {
                                Date = vt.Timestamp,
                                Type = vt.ServiceType.ToString(),
                                Document = "VAS-" + vt.Id.ToString().Substring(0, 8).ToUpper(),
                                MaterialName = m.Name,
                                MaterialId = vl.MaterialId.Value,
                                QuantityIn = 0m,
                                QuantityOut = vl.Quantity,
                                WeightIn = 0m,
                                WeightOut = vl.Weight,
                                AccountId = vt.AccountId,
                                SupplierId = null,
                                TruckId = null,
                                UserId = vt.UserId
                            };

        var vasOutputQuery = from vt in context.VASTransactions.AsNoTracking()
                       .Where(vt =>
                         vt.ServiceType == ServiceType.Repack ||
                         vt.ServiceType == ServiceType.Split ||
                         vt.ServiceType == ServiceType.Kitting)
                             join vl in context.VASTransactionLines on vt.Id equals vl.VASTransactionId      
                             join m in context.Materials on vl.MaterialId.Value equals m.Id
                             where !vl.IsInput && vl.MaterialId.HasValue
                             select new LedgerEntryIntermediate
                             {
                                 Date = vt.Timestamp,
                                 Type = vt.ServiceType.ToString(),
                                 Document = "VAS-" + vt.Id.ToString().Substring(0, 8).ToUpper(),
                                 MaterialName = m.Name,
                                 MaterialId = vl.MaterialId.Value,
                                 QuantityIn = vl.Quantity,
                                 QuantityOut = 0m,
                                 WeightIn = vl.Weight,
                                 WeightOut = 0m,
                                 AccountId = vt.AccountId,
                                 SupplierId = null,
                                 TruckId = null,
                                 UserId = vt.UserId
                             };

        var adjustmentQuery = context.Set<InventoryAdjustment>().AsNoTracking()
            .Include(adj => adj.Inventory.Material)
            .Select(adj => new LedgerEntryIntermediate
            {
                Date = adj.Timestamp,
                Type = adj.Reason.ToString(),
                Document = "ADJ-" + adj.Id.ToString().Substring(0, 8).ToUpper(),
                MaterialName = adj.Inventory.Material.Name,
                MaterialId = adj.Inventory.MaterialId,
                QuantityIn = adj.DeltaQuantity > 0 ? adj.DeltaQuantity : 0m,
                QuantityOut = adj.DeltaQuantity < 0 ? -adj.DeltaQuantity : 0m,
                WeightIn = 0m,       
                WeightOut = 0m,
                AccountId = adj.AccountId,
                SupplierId = null,
                TruckId = null,
                UserId = adj.UserId
            });

        var transferOutQuery = context.ItemTransferTransactions.AsNoTracking()
             .Include(t => t.SourceInventory.Material)     
             .Select(t => new LedgerEntryIntermediate
             {
                 Date = t.Timestamp,
                 Type = "Item Transfer",
                 Document = "XFER-" + t.Id.ToString().Substring(0, 8).ToUpper(),
                 MaterialName = t.SourceInventory.Material.Name,
                 MaterialId = t.SourceInventory.MaterialId,
                 QuantityIn = 0m,
                 QuantityOut = t.QuantityTransferred,
                 WeightIn = 0m,
                 WeightOut = t.WeightTransferred,
                 AccountId = t.SourceInventory.AccountId,
                 SupplierId = null,
                 TruckId = null,
                 UserId = t.UserId
             });

        var transferInQuery = context.ItemTransferTransactions.AsNoTracking()
             .Include(t => t.SourceInventory.Material)     
             .Select(t => new LedgerEntryIntermediate
             {
                 Date = t.Timestamp,
                 Type = "Item Transfer",
                 Document = "XFER-" + t.Id.ToString().Substring(0, 8).ToUpper(),
                 MaterialName = t.SourceInventory.Material.Name,         
                 MaterialId = t.SourceInventory.MaterialId,
                 QuantityIn = t.QuantityTransferred,
                 QuantityOut = 0m,
                 WeightIn = t.WeightTransferred,
                 WeightOut = 0m,
                 AccountId = t.SourceInventory.AccountId,
                 SupplierId = null,
                 TruckId = null,
                 UserId = t.UserId
             });

        var combinedQuery = receivingQuery
            .Union(pickingQuery)
            .Union(vasInputQuery)
            .Union(vasOutputQuery)
            .Union(adjustmentQuery)
            .Union(transferOutQuery)
            .Union(transferInQuery);

        if (filter.StartDate.HasValue) combinedQuery = combinedQuery.Where(e => e.Date >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
        {
            var endDateEndOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            combinedQuery = combinedQuery.Where(e => e.Date <= endDateEndOfDay);
        }
        if (filter.AccountId.HasValue) combinedQuery = combinedQuery.Where(e => e.AccountId == filter.AccountId.Value);
        if (filter.MaterialId.HasValue) combinedQuery = combinedQuery.Where(e => e.MaterialId == filter.MaterialId.Value);
        if (filter.SupplierId.HasValue) combinedQuery = combinedQuery.Where(e => e.SupplierId == filter.SupplierId.Value);
        if (filter.TruckId.HasValue) combinedQuery = combinedQuery.Where(e => e.TruckId == filter.TruckId.Value);
        if (filter.UserId.HasValue) combinedQuery = combinedQuery.Where(e => e.UserId == filter.UserId.Value);

        var materialNamesQuery = combinedQuery.Select(e => e.MaterialName).Distinct();

        var totalMaterialCount = await materialNamesQuery.CountAsync(cancellationToken);

        IQueryable<string> orderedMaterialNames;      
        if (!string.IsNullOrWhiteSpace(filter.SortBy) && filter.SortBy.Equals("MaterialName", StringComparison.OrdinalIgnoreCase))
        {
            var sortOrder = filter.SortDirection?.ToLower() == "desc" ? "descending" : "ascending";
            orderedMaterialNames = materialNamesQuery.OrderBy($"it {sortOrder}");     
        }
        else
        {
            orderedMaterialNames = materialNamesQuery.OrderBy(name => name);   
        }

        var pagedMaterialNames = await orderedMaterialNames
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        if (!pagedMaterialNames.Any())
        {
            return new Application.Common.Models.PagedResult<InventoryLedgerGroupDto> { Items = new List<InventoryLedgerGroupDto>(), TotalCount = totalMaterialCount };
        }

        var filteredLedgerEntries = await combinedQuery
            .Where(e => pagedMaterialNames.Contains(e.MaterialName))
            .OrderBy(e => e.MaterialName).ThenBy(e => e.Date)       
            .ToListAsync(cancellationToken);

        var groupedLedger = filteredLedgerEntries
            .GroupBy(e => e.MaterialName)
            .Select(g => {
                decimal runningQty = 0;
                decimal runningWgt = 0;
                var lines = g.OrderBy(e => e.Date)
                             .Select(e => {
                                 runningQty += e.QuantityIn - e.QuantityOut;
                                 runningWgt += e.WeightIn - e.WeightOut;
                                 return new InventoryLedgerLineDto
                                 {
                                     Date = e.Date,
                                     Type = e.Type,
                                     Document = e.Document,
                                     QuantityIn = e.QuantityIn,
                                     QuantityOut = e.QuantityOut,
                                     WeightIn = e.WeightIn,
                                     WeightOut = e.WeightOut,
                                     RunningBalanceQty = runningQty,
                                     RunningBalanceWgt = runningWgt
                                 };
                             }).ToList();

                return new InventoryLedgerGroupDto
                {
                    MaterialName = g.Key,
                    TotalQtyIn = g.Sum(e => e.QuantityIn),
                    TotalQtyOut = g.Sum(e => e.QuantityOut),
                    TotalWgtIn = g.Sum(e => e.WeightIn),
                    TotalWgtOut = g.Sum(e => e.WeightOut),
                    Lines = lines
                };
            })
            .OrderBy(dto => pagedMaterialNames.IndexOf(dto.MaterialName))
            .ToList();

        return new Application.Common.Models.PagedResult<InventoryLedgerGroupDto> { Items = groupedLedger, TotalCount = totalMaterialCount };
    }

    public async Task<Application.Common.Models.PagedResult<ActivityLogDto>> GetActivityLogAsync(GetActivityLogQuery filter, CancellationToken cancellationToken)
    {
        var receivingQuery = context.Receivings.AsNoTracking()
            .Select(t => new RawLogData
            {
                Timestamp = t.Timestamp,
                Action = "Receiving",
                UserId = t.CreatedBy,
                AccountId = t.AccountId,
                Arg1 = t.Supplier != null ? t.Supplier.Name : "N/A",     
                Arg2 = null,
                Arg3 = null,
                Value1 = null,
                TruckId = t.Appointment != null ? t.Appointment.TruckId : null,
                Description = null
            });

        var putawayQuery = context.PutawayTransactions.AsNoTracking()
            .Select(t => new RawLogData
            {
                Timestamp = t.Timestamp,
                Action = "Put Away",
                UserId = t.UserId,
                AccountId = t.Pallet != null ? t.Pallet.AccountId : Guid.Empty,     
                Arg1 = t.Pallet != null ? t.Pallet.Barcode : "N/A",
                Arg2 = t.Location != null ? t.Location.Barcode : "N/A",
                Arg3 = null,
                Value1 = t.Pallet != null ? t.Pallet.Lines.Count() : 0,              
                TruckId = t.Pallet != null && t.Pallet.Receiving != null && t.Pallet.Receiving.Appointment != null ? t.Pallet.Receiving.Appointment.TruckId : null,
                Description = null
            });

        var pickingQuery = context.PickTransactions.AsNoTracking()
            .Select(t => new RawLogData
            {
                Timestamp = t.Timestamp,
                Action = "Picking",
                UserId = t.UserId,
                AccountId = t.AccountId,
                Arg1 = t.MaterialInventory != null && t.MaterialInventory.Material != null ? t.MaterialInventory.Material.Name : "N/A",
                Arg2 = t.MaterialInventory != null && t.MaterialInventory.Location != null ? t.MaterialInventory.Location.Barcode : "N/A",
                Arg3 = null,
                Value1 = t.Quantity,
                TruckId = t.WithdrawalTransactions.Select(wt => wt.Appointment != null ? wt.Appointment.TruckId : (Guid?)null).FirstOrDefault(),
                Description = null
            });

        var shippingQuery = context.WithdrawalTransactions.AsNoTracking()
             .Select(t => new RawLogData
             {
                 Timestamp = t.Timestamp,
                 Action = "Shipping",
                 UserId = t.Picks.Select(p => (Guid?)p.UserId).FirstOrDefault(),
                 AccountId = t.AccountId,
                 Arg1 = t.ShipmentNumber,
                 Arg2 = (t.Appointment != null && t.Appointment.Truck != null) ? t.Appointment.Truck.LicensePlate : "N/A",
                 Arg3 = null,
                 Value1 = null,
                 TruckId = t.Appointment != null ? t.Appointment.TruckId : null,
                 Description = null
             });

        var vasQuery = context.VASTransactions.AsNoTracking()
             .Select(t => new RawLogData
             {
                 Timestamp = t.Timestamp,
                 Action = t.ServiceType.ToString(),
                 UserId = t.UserId,
                 AccountId = t.AccountId,
                 Arg1 = null,
                 Arg2 = null,
                 Arg3 = null,
                 Value1 = null,
                 TruckId = t.PalletId.HasValue ? context.Pallets.Where(p => p.Id == t.PalletId).Select(p => p.Receiving.Appointment != null ? p.Receiving.Appointment.TruckId : (Guid?)null).FirstOrDefault() : null,
                 Description = t.Description        
             });

        var transferQuery = context.TransferTransactions.AsNoTracking()
             .Select(t => new RawLogData
             {
                 Timestamp = t.Timestamp,
                 Action = "Transfer",
                 UserId = t.UserId,
                 AccountId = t.Pallet != null ? t.Pallet.AccountId : Guid.Empty,
                 Arg1 = t.Pallet != null ? t.Pallet.Barcode : "N/A",
                 Arg2 = t.FromLocation != null ? t.FromLocation.Barcode : "N/A",
                 Arg3 = t.ToLocation != null ? t.ToLocation.Barcode : "N/A",
                 Value1 = null,
                 TruckId = null,       
                 Description = null
             });

        var itemTransferQuery = context.ItemTransferTransactions.AsNoTracking()
            .Select(t => new RawLogData
            {
                Timestamp = t.Timestamp,
                Action = "Item Transfer",
                UserId = t.UserId,
                AccountId = t.SourceInventory != null ? t.SourceInventory.AccountId : Guid.Empty,
                Arg1 = t.SourceInventory != null && t.SourceInventory.Pallet != null ? t.SourceInventory.Pallet.Barcode : "N/A",   
                Arg2 = t.NewDestinationPallet != null ? t.NewDestinationPallet.Barcode : "N/A",     
                Arg3 = t.SourceInventory != null && t.SourceInventory.Material != null ? t.SourceInventory.Material.Name : "N/A",     
                Value1 = t.QuantityTransferred,
                TruckId = null,       
                Description = null
            });

        var activityAdjustmentQuery = context.Set<InventoryAdjustment>().AsNoTracking()
            .Select(adj => new RawLogData
            {
                Timestamp = adj.Timestamp,
                Action = adj.Reason.ToString(),
                UserId = adj.UserId,
                AccountId = adj.AccountId,
                Arg1 = adj.Inventory != null && adj.Inventory.Material != null ? adj.Inventory.Material.Name : "N/A",
                Arg2 = adj.Inventory != null ? adj.Inventory.Barcode : "N/A",  
                Arg3 = adj.DeltaQuantity > 0 ? "Added" : "Removed",
                Value1 = adj.DeltaQuantity,
                TruckId = null,     
                Description = null
            });

        var combinedQuery = receivingQuery
            .Union(putawayQuery)
            .Union(pickingQuery)
            .Union(shippingQuery)
            .Union(vasQuery)
            .Union(transferQuery)
            .Union(itemTransferQuery)
            .Union(activityAdjustmentQuery);

        if (filter.StartDate.HasValue) combinedQuery = combinedQuery.Where(a => a.Timestamp >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
        {
            var endDateEndOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            combinedQuery = combinedQuery.Where(a => a.Timestamp <= endDateEndOfDay);
        }
        if (filter.AccountId.HasValue) combinedQuery = combinedQuery.Where(a => a.AccountId == filter.AccountId.Value);
        if (filter.UserId.HasValue) combinedQuery = combinedQuery.Where(a => a.UserId == filter.UserId.Value);
        if (filter.TruckId.HasValue) combinedQuery = combinedQuery.Where(a => a.TruckId == filter.TruckId.Value);


        var totalCount = await combinedQuery.CountAsync(cancellationToken);

        var orderedQuery = combinedQuery.OrderByDescending(a => a.Timestamp);

        var pagedRawItems = await orderedQuery
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = pagedRawItems.Where(i => i.UserId.HasValue).Select(i => i.UserId!.Value).Distinct().ToList();
        var accountIds = pagedRawItems.Where(i => i.AccountId.HasValue).Select(i => i.AccountId!.Value).Distinct().ToList();

        var users = userIds.Any()
            ? await context.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken)
            : new Dictionary<Guid, string>();
        var accounts = accountIds.Any()
            ? await context.Accounts.AsNoTracking().Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var pagedItems = pagedRawItems.Select(raw => new ActivityLogDto
        {
            Timestamp = raw.Timestamp,
            User = raw.UserId.HasValue && users.TryGetValue(raw.UserId.Value, out var userName) ? userName : "System",
            Action = raw.Action,
            Description = FormatActivityDescription(raw),   
            Account = raw.AccountId.HasValue && accounts.TryGetValue(raw.AccountId.Value, out var accountName) ? accountName : "N/A"
        }).ToList();

        return new Application.Common.Models.PagedResult<ActivityLogDto> { Items = pagedItems, TotalCount = totalCount };
    }

    public async Task<Application.Common.Models.PagedResult<StockOnHandDto>> GetStockOnHandAsync(GetStockOnHandQuery request, CancellationToken cancellationToken)
    {
        var query = context.MaterialInventories.AsNoTracking()
               .Where(mi => mi.Quantity > 0);

        if (request.AccountId.HasValue)
        {
            query = query.Where(mi => mi.AccountId == request.AccountId.Value);
        }
        if (request.MaterialId.HasValue)
        {
            query = query.Where(mi => mi.MaterialId == request.MaterialId.Value);
        }
        if (request.SupplierId.HasValue)
        {
            query = query.Where(mi => mi.Pallet.Receiving.SupplierId == request.SupplierId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.BatchNumber))
        {
            query = query.Where(mi => mi.BatchNumber != null && mi.BatchNumber.Contains(request.BatchNumber));
        }
        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            query = query.Where(mi => (mi.Barcode != null && mi.Barcode.Contains(request.Barcode)) || (mi.Pallet.Barcode != null && mi.Pallet.Barcode.Contains(request.Barcode)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortColumn = request.SortBy switch
            {
                "materialName" => "Material.Name",
                "sku" => "Material.Sku",
                "palletBarcode" => "Pallet.Barcode",
                "lpnBarcode" => "Barcode",
                "batchNumber" => "BatchNumber",
                "location" => "Location.Barcode",
                "room" => "Location.Room.Name",
                "accountName" => "Pallet.Account.Name",          
                "supplierName" => "Pallet.Receiving.Supplier.Name",        
                "expiryDate" => "ExpiryDate",
                "quantity" => "Quantity",
                "weight" => "WeightActual.Value",     
                _ => "Material.Name"   
            };

            var sortDirection = request.SortDirection?.ToLower() == "desc" ? "descending" : "ascending";
            query = query.OrderBy($"{sortColumn} {sortDirection}");
        }
        else
        {
            query = query.OrderBy(mi => mi.Material.Name).ThenBy(mi => mi.ExpiryDate);
        }

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(mi => new StockOnHandDto
            {
                MaterialInventoryId = mi.Id,
                MaterialName = mi.Material.Name,    
                Sku = mi.Material.Sku,
                PalletBarcode = mi.Pallet.Barcode,    
                LpnBarcode = mi.Barcode ?? "",
                BatchNumber = mi.BatchNumber ?? "",
                Location = mi.Location.Barcode,    
                Room = mi.Location.Room.Name,    
                AccountName = mi.Pallet.Account.Name,    
                SupplierName = mi.Pallet.Receiving.Supplier.Name,    
                Quantity = mi.Quantity,
                Weight = mi.WeightActual.Value,     
                ExpiryDate = mi.ExpiryDate
            })
            .ToListAsync(cancellationToken);

        return new Application.Common.Models.PagedResult<StockOnHandDto> { Items = items, TotalCount = totalCount };
    }

    [Obsolete("Use GetDailyPalletCountByZoneAsync instead to differentiate storage types.")]
    public async Task<Dictionary<DateTime, int>> GetDailyPalletCountAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var palletLifespans = new Dictionary<Guid, (DateTime Start, DateTime End)>();

        var putaways = await context.PutawayTransactions
            .AsNoTracking()
            .Where(pt => context.Pallets.Any(p => p.Id == pt.PalletId && p.AccountId == accountId))
            .Select(pt => new { pt.PalletId, pt.Timestamp })
            .ToDictionaryAsync(x => x.PalletId, x => x.Timestamp, cancellationToken);

        var firstPicks = await context.PickTransactions
            .AsNoTracking()
            .Where(pt => pt.PalletId.HasValue && putaways.Keys.Contains(pt.PalletId.Value))
            .GroupBy(pt => pt.PalletId)
            .Select(g => new { PalletId = g.Key, FirstPickDate = g.Min(pt => pt.Timestamp) })
            .ToDictionaryAsync(x => x.PalletId!.Value, x => x.FirstPickDate, cancellationToken);

        foreach (var putaway in putaways)
        {
            var palletId = putaway.Key;
            var entryDate = putaway.Value;
            var exitDate = firstPicks.TryGetValue(palletId, out var pickDate) ? pickDate : endDate.AddDays(1);     
            palletLifespans[palletId] = (entryDate.Date, exitDate.Date);
        }

        var dailyCounts = new Dictionary<DateTime, int>();
        for (var day = startDate.Date; day < endDate.Date; day = day.AddDays(1))
        {
            int count = palletLifespans.Count(lifespan => lifespan.Value.Start <= day && day < lifespan.Value.End);
            dailyCounts[day] = count;
        }

        return dailyCounts;
    }

    public async Task<Dictionary<ServiceType, int>> GetDailyPalletCountByZoneAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var dailyCountsByZone = Enum.GetValues<ServiceType>()
                                    .Where(st => StorageTierMap.ContainsKey(st))
                                    .ToDictionary(st => st, _ => 0);
        dailyCountsByZone[ServiceType.Staging] = 0;     

        var relevantPallets = await context.Pallets
            .AsNoTracking()
            .Where(p => p.AccountId == accountId && p.Receiving.Timestamp < endDate)      
            .Select(p => new { p.Id, EntryDate = p.Receiving.Timestamp })
            .ToListAsync(cancellationToken);

        if (!relevantPallets.Any()) return dailyCountsByZone;

        var palletIds = relevantPallets.Select(p => p.Id).ToList();

        var putawayInfos = await context.PutawayTransactions
            .AsNoTracking()
            .Where(pt => palletIds.Contains(pt.PalletId))
            .GroupBy(pt => pt.PalletId)
            .Select(g => g.OrderBy(pt => pt.Timestamp).Select(pt => new { pt.PalletId, pt.Timestamp, pt.Location.Room.ServiceType }).First())
            .ToDictionaryAsync(x => x.PalletId, x => new { x.Timestamp, x.ServiceType }, cancellationToken);

        var firstPickDates = await context.PickTransactions
            .AsNoTracking()
            .Where(pt => pt.PalletId.HasValue && palletIds.Contains(pt.PalletId.Value))
            .GroupBy(pt => pt.PalletId.Value)
            .Select(g => new { PalletId = g.Key, FirstPickDate = g.Min(pt => (DateTime?)pt.Timestamp) })       
            .ToDictionaryAsync(x => x.PalletId, x => x.FirstPickDate, cancellationToken);      

        for (var day = startDate.Date; day < endDate.Date; day = day.AddDays(1))
        {
            var startOfDay = day;
            var endOfDay = day.AddDays(1);

            foreach (var palletInfo in relevantPallets)
            {
                var entryDate = palletInfo.EntryDate;
                firstPickDates.TryGetValue(palletInfo.Id, out DateTime? firstPickDate);
                var effectiveExitDate = firstPickDate ?? endDate.Date.AddDays(1);      

                bool isPresentOnDay = entryDate < endOfDay && effectiveExitDate > startOfDay;
                if (!isPresentOnDay) continue;

                ServiceType zoneForBilling = ServiceType.Staging;
                if (putawayInfos.TryGetValue(palletInfo.Id, out var putaway) && putaway.Timestamp < endOfDay)
                {
                    if (StorageTierMap.ContainsKey(putaway.ServiceType))
                    {
                        zoneForBilling = putaway.ServiceType;
                    }
                }

                if (dailyCountsByZone.ContainsKey(zoneForBilling))
                {
                    dailyCountsByZone[zoneForBilling]++;
                }
            }
        }

        return dailyCountsByZone;
    }

    public async Task<Dictionary<ServiceType, decimal>> GetDailyWeightByZoneAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var dailyWeightByZone = Enum.GetValues<ServiceType>()
                                    .Where(st => StorageTierMap.ContainsKey(st))
                                    .ToDictionary(st => st, _ => 0m);
        dailyWeightByZone[ServiceType.Staging] = 0m;     

        var relevantInventoryItems = await context.MaterialInventories
            .AsNoTracking()
            .Include(mi => mi.Pallet.Receiving)
            .Where(mi => mi.AccountId == accountId
                         && mi.Pallet.Receiving.Timestamp < endDate
                         && mi.Quantity > 0
                       )
            .Select(mi => new
            {
                mi.Id,
                mi.PalletId,
                mi.WeightActual,
                EntryDate = mi.Pallet.Receiving.Timestamp
            })
            .ToListAsync(cancellationToken);

        if (!relevantInventoryItems.Any()) return dailyWeightByZone;

        var inventoryIds = relevantInventoryItems.Select(i => i.Id).ToList();
        var palletIds = relevantInventoryItems.Select(i => i.PalletId).Distinct().ToList();

        var putawayInfos = await context.PutawayTransactions
            .AsNoTracking()
            .Where(pt => palletIds.Contains(pt.PalletId))
            .GroupBy(pt => pt.PalletId)
            .Select(g => g.OrderBy(pt => pt.Timestamp).Select(pt => new { pt.PalletId, pt.Timestamp, pt.Location.Room.ServiceType }).First())
            .ToDictionaryAsync(x => x.PalletId, x => new { x.Timestamp, x.ServiceType }, cancellationToken);

        var firstPickDatesByInventory = await context.PickTransactions
            .AsNoTracking()
            .Where(pt => inventoryIds.Contains(pt.InventoryId))
            .GroupBy(pt => pt.InventoryId)
            .Select(g => new { InventoryId = g.Key, FirstPickDate = g.Min(pt => (DateTime?)pt.Timestamp) })       
            .ToDictionaryAsync(x => x.InventoryId, x => x.FirstPickDate, cancellationToken);      

        for (var day = startDate.Date; day < endDate.Date; day = day.AddDays(1))
        {
            var startOfDay = day;
            var endOfDay = day.AddDays(1);

            foreach (var itemInfo in relevantInventoryItems)
            {
                var entryDate = itemInfo.EntryDate;
                firstPickDatesByInventory.TryGetValue(itemInfo.Id, out DateTime? firstPickDate);
                var effectiveExitDate = firstPickDate ?? endDate.Date.AddDays(1);      

                bool isPresentOnDay = entryDate < endOfDay && effectiveExitDate > startOfDay;
                if (!isPresentOnDay) continue;

                decimal itemWeight = itemInfo.WeightActual.Value;
                ServiceType zoneForBilling = ServiceType.Staging;

                if (putawayInfos.TryGetValue(itemInfo.PalletId, out var putaway) && putaway.Timestamp < endOfDay)
                {
                    if (StorageTierMap.ContainsKey(putaway.ServiceType))
                    {
                        zoneForBilling = putaway.ServiceType;
                    }
                }

                if (dailyWeightByZone.ContainsKey(zoneForBilling))
                {
                    dailyWeightByZone[zoneForBilling] += itemWeight;
                }
            }
        }
        return dailyWeightByZone;
    }

    private class LedgerEntryDto       
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal WeightIn { get; set; }
        public decimal WeightOut { get; set; }
        public Guid AccountId { get; set; }
        public Guid MaterialId { get; set; }
        public Guid? SupplierId { get; set; }
        public Guid? TruckId { get; set; }
        public Guid? UserId { get; set; }
    }

    private string FormatActivityDescription(RawLogData raw)
    {
        return raw.Action switch
        {
            "Receiving" => $"Started receiving session for supplier '{raw.Arg1}'",
            "Put Away" => $"Put away pallet {raw.Arg1} with {raw.Value1?.ToString("N0") ?? "N/A"} item(s) to location {raw.Arg2}",
            "Picking" => $"Picked {raw.Value1?.ToString("N0") ?? "N/A"} of '{raw.Arg1}' from location {raw.Arg2}",
            "Shipping" => $"Shipped order '{raw.Arg1}' on truck '{raw.Arg2}'",
            "Transfer" => $"Transferred pallet {raw.Arg1} from location {raw.Arg2} to {raw.Arg3}",
            "Item Transfer" => $"Transferred {raw.Value1?.ToString("N0") ?? "N/A"} of '{raw.Arg3}' from pallet {raw.Arg1} to new pallet {raw.Arg2}",
            "Count" => $"Cycle Count: {raw.Arg3} {Math.Abs(raw.Value1 ?? 0):N0} units of '{raw.Arg1}' (LPN: {raw.Arg2})",
            "Damage" => $"Damage adjustment: {raw.Arg3} {Math.Abs(raw.Value1 ?? 0):N0} units of '{raw.Arg1}' (LPN: {raw.Arg2})",
            "Expiry" => $"Expiry adjustment: {raw.Arg3} {Math.Abs(raw.Value1 ?? 0):N0} units of '{raw.Arg1}' (LPN: {raw.Arg2})",
            _ => raw.Description ?? raw.Arg1 ?? $"Performed {raw.Action} transaction"       
        };
    }
}