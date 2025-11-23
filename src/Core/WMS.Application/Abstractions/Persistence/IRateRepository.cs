using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Abstractions.Persistence;

public interface IRateRepository
{
    Task<Rate?> GetRateForAccountAsync(Guid accountId, ServiceType serviceType, RateUom uom, string? tier, CancellationToken cancellationToken);
    Task<IEnumerable<Rate>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Rate rate, CancellationToken cancellationToken);
    Task<Rate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Remove(Rate rate);
    Task<Common.Models.PagedResult<Features.Admin.Queries.RateDto>> GetPagedListAsync(Features.Admin.Queries.GetRatesQuery request, CancellationToken cancellationToken);
}