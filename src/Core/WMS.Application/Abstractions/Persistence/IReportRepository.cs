using WMS.Application.Common.Models;
using WMS.Application.Features.Reports.Queries;    
using WMS.Domain.Enums;    

namespace WMS.Application.Abstractions.Persistence;

public interface IReportRepository
{
    Task<PagedResult<InventoryLedgerGroupDto>> GetInventoryLedgerAsync(GetInventoryLedgerQuery filter, CancellationToken cancellationToken);

    Task<PagedResult<StockOnHandDto>> GetStockOnHandAsync(GetStockOnHandQuery query, CancellationToken cancellationToken);
    [Obsolete("Use GetDailyPalletCountByZoneAsync instead to differentiate storage types.")]
    Task<Dictionary<DateTime, int>> GetDailyPalletCountAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

    Task<Dictionary<ServiceType, int>> GetDailyPalletCountByZoneAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<Dictionary<ServiceType, decimal>> GetDailyWeightByZoneAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<PagedResult<ActivityLogDto>> GetActivityLogAsync(GetActivityLogQuery filter, CancellationToken cancellationToken);
}