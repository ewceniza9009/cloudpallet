using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Commands;

// --- Command Updated ---
public record CreateSupplierCommand(
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
    decimal CreditLimit) : IRequest<Guid>;

public class CreateSupplierCommandHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSupplierCommand, Guid>
{
    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = Supplier.Create(request.Name);

        supplier.UpdateContactInfo(request.ContactName, request.Phone, request.Email, request.Address);
        supplier.UpdateGeneral(
            request.Name,
            request.Description,
            request.TaxId,
            request.LeadTimeDays,
            request.PaymentTerms,
            request.CreditLimit,
            true // IsActive
        );
        // You would also update CertificationColdChain, CurrencyCode if methods exist
        // e.g. supplier.SetCertification(request.CertificationColdChain);
        // e.g. supplier.SetCurrency(request.CurrencyCode);

        await supplierRepository.AddAsync(supplier, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return supplier.Id;
    }
}