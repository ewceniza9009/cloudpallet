using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;
using WMS.Application.Features.WarehouseSetup.Queries;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class AccountRepository(WmsDbContext context) : IAccountRepository
{
    public Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        context.Accounts.Add(account);
        return Task.CompletedTask;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Accounts
            .Include(a => a.Address)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Application.Common.Models.PagedResult<AccountDto>> GetPagedListAsync(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Accounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(term) || (a.ContactName != null && a.ContactName.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortColumn = request.SortBy switch
            {
                "name" => "Name",
                "phone" => "Phone",
                "city" => "Address.City",
                "isActive" => "IsActive",
                _ => "Name"
            };
            var sortDirection = request.SortDirection?.ToLower() == "desc" ? "descending" : "ascending";
            query = query.OrderBy($"{sortColumn} {sortDirection}");
        }
        else
        {
            query = query.OrderBy(a => a.Name);   
        }

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AccountDto(
                a.Id,
                a.Name,
                a.Phone,        
                a.Address != null ? a.Address.City : null,
                a.IsActive
            ))
            .ToListAsync(cancellationToken);

        return new Application.Common.Models.PagedResult<AccountDto> { Items = items, TotalCount = totalCount };
    }

    public void Remove(Account account)
    {
        context.Accounts.Remove(account);
    }
}