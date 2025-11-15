using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdateAccountCommand(
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
    decimal CreditLimit) : IRequest;

public class UpdateAccountCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAccountCommand>
{
    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Account with ID {request.Id} not found.");

        account.UpdateGeneral(
            request.Name,
            request.TypeId,
            request.TaxId,
            request.PaymentTerms,
            request.CreditLimit,
            request.IsActive,
            request.CategoryId,
            request.PreferredTempZone,
            request.IsPreferred
        );

        account.UpdateContactInfo(
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}