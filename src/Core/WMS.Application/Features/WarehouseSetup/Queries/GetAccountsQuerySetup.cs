using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record AccountDto(
    Guid Id,
    string Name,
    string? Phone,
    string? City,
    bool IsActive);

public record GetAccountsQuery : IRequest<PagedResult<AccountDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public string? SearchTerm { get; init; }
}

public class GetAccountsQueryHandler(IAccountRepository accountRepository)
    : IRequestHandler<GetAccountsQuery, PagedResult<AccountDto>>
{
    public async Task<PagedResult<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        return await accountRepository.GetPagedListAsync(request, cancellationToken);
    }
}