using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record AccountDetailDto(
    Guid Id,
    string Name,
    AccountType TypeId,
    Guid? CategoryId,
    Address Address,
    string ContactName,
    string Phone,
    string Email,
    string TaxId,
    string PaymentTerms,
    string CurrencyCode,
    TempZone? PreferredTempZone,
    bool IsPreferred,
    bool IsActive,
    decimal CreditLimit);

public record GetAccountByIdQuery(Guid Id) : IRequest<AccountDetailDto?>;

public class GetAccountByIdQueryHandler(IAccountRepository accountRepository)
    : IRequestHandler<GetAccountByIdQuery, AccountDetailDto?>
{
    public async Task<AccountDetailDto?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.Id, cancellationToken);
        if (account == null) return null;

        return new AccountDetailDto(
            account.Id,
            account.Name,
            account.TypeId,
            account.CategoryId,
            account.Address,
            account.ContactName,
            account.Phone,
            account.Email,
            account.TaxId,
            account.PaymentTerms,
            account.CurrencyCode,
            account.PreferredTempZone,
            account.IsPreferred,
            account.IsActive,
            account.CreditLimit
        );
    }
}