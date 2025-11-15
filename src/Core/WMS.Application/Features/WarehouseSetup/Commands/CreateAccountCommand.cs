using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record CreateAccountCommand(
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
    decimal CreditLimit) : IRequest<Guid>;

public class CreateAccountCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = Account.Create(request.Name, request.TypeId);

        account.UpdateContactInfo(request.ContactName, request.Phone, request.Email, request.Address);
        account.UpdateGeneral(
            request.Name,
            request.TypeId,
            request.TaxId,
            request.PaymentTerms,
            request.CreditLimit,
            true,  
            request.CategoryId,
            request.PreferredTempZone,
            request.IsPreferred
        );
        await accountRepository.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}