using WMS.Application.Common.Models;
using WMS.Application.Features.WarehouseSetup.Queries;   
using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<AccountDto>> GetPagedListAsync(GetAccountsQuery query, CancellationToken cancellationToken);
    void Remove(Account account);
}