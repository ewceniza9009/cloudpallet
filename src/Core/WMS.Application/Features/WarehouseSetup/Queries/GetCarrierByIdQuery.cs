using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Queries;

// We'll re-use the DTO, but include more fields
public record CarrierDetailDto(
    Guid Id,
    string Name,
    string ScacCode,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    Address? Address,
    bool CertificationColdChain,
    string? InsurancePolicyNumber,
    DateTime? InsuranceExpiryDate,
    bool IsActive);

public record GetCarrierByIdQuery(Guid Id) : IRequest<CarrierDetailDto?>;

public class GetCarrierByIdQueryHandler(ICarrierRepository carrierRepository)
    : IRequestHandler<GetCarrierByIdQuery, CarrierDetailDto?>
{
    public async Task<CarrierDetailDto?> Handle(GetCarrierByIdQuery request, CancellationToken cancellationToken)
    {
        var carrier = await carrierRepository.GetByIdAsync(request.Id, cancellationToken);
        if (carrier == null) return null;

        return new CarrierDetailDto(
            carrier.Id,
            carrier.Name,
            carrier.ScacCode,
            carrier.ContactName,
            carrier.ContactPhone,
            carrier.ContactEmail,
            carrier.Address,
            carrier.CertificationColdChain,
            carrier.InsurancePolicyNumber,
            carrier.InsuranceExpiryDate,
            carrier.IsActive
        );
    }
}