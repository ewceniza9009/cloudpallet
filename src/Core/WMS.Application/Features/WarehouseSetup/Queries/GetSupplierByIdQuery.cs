using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record SupplierDetailDto(
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
    decimal CreditLimit);

public record GetSupplierByIdQuery(Guid Id) : IRequest<SupplierDetailDto?>;

public class GetSupplierByIdQueryHandler(ISupplierRepository supplierRepository)
    : IRequestHandler<GetSupplierByIdQuery, SupplierDetailDto?>
{
    public async Task<SupplierDetailDto?> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.GetByIdAsync(request.Id, cancellationToken);
        if (supplier == null) return null;

        return new SupplierDetailDto(
            supplier.Id,
            supplier.Name,
            supplier.Description,
            supplier.Address,
            supplier.ContactName,
            supplier.Phone,
            supplier.Email,
            supplier.TaxId,
            supplier.LeadTimeDays,
            supplier.CertificationColdChain,
            supplier.PaymentTerms,
            supplier.CurrencyCode,
            supplier.IsActive,
            supplier.CreditLimit
        );
    }
}