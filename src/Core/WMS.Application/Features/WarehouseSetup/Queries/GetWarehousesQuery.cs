using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Companies.Queries;   

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record GetWarehousesQuery : IRequest<IEnumerable<WarehouseDto>>;

public class GetWarehousesQueryHandler(IWarehouseAdminRepository repository)
    : IRequestHandler<GetWarehousesQuery, IEnumerable<WarehouseDto>>
{
    public async Task<IEnumerable<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await repository.GetAllAsync(cancellationToken);

        return warehouses.Select(w => new WarehouseDto(
            w.Id,
            w.CompanyId,
            w.Name,
            new AddressDto(w.Address.Street, w.Address.City, w.Address.State, w.Address.PostalCode, w.Address.Country),
            w.OperatingHours,
            w.ContactPhone,
            w.ContactEmail,
            w.IsActive
        )).ToList();
    }
}