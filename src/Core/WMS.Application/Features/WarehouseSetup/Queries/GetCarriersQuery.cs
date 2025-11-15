using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record CarrierDto(
    Guid Id,
    string Name,
    string ScacCode,
    string? ContactName,
    string? ContactPhone,
    Address? Address,
    bool IsActive,
    int TruckCount);    

public record GetCarriersQuery : IRequest<IEnumerable<CarrierDto>>;

public class GetCarriersQueryHandler(ICarrierRepository carrierRepository)
    : IRequestHandler<GetCarriersQuery, IEnumerable<CarrierDto>>
{
    public async Task<IEnumerable<CarrierDto>> Handle(GetCarriersQuery request, CancellationToken cancellationToken)
    {
        var carriers = await carrierRepository.GetAllWithTrucksAsync(cancellationToken);     

        return carriers.Select(c => new CarrierDto(
            c.Id,
            c.Name,
            c.ScacCode,
            c.ContactName,
            c.ContactPhone,
            c.Address,
            c.IsActive,
            c.Trucks.Count      
        )).OrderBy(c => c.Name);
    }
}