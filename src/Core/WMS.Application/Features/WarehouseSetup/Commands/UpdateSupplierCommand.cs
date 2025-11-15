using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string Description,
    Address Address,
    string ContactName,
    string Phone,
    string Email,
    string TaxId,
    int LeadTimeDays,
    bool CertificationColdChain,
    string PaymentTerms,
    string CurrencyCode,
    bool IsActive,
    decimal CreditLimit) : IRequest;

public class UpdateSupplierCommandHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSupplierCommand>
{
    public async Task Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier with ID {request.Id} not found.");

        supplier.UpdateGeneral(
            request.Name,
            request.Description,
            request.TaxId,
            request.LeadTimeDays,
            request.PaymentTerms,
            request.CreditLimit,
            request.IsActive
        );

        supplier.UpdateContactInfo(
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}