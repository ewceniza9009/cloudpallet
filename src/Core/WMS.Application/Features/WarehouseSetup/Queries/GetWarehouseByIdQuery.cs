using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Companies.Queries;   

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record GetWarehouseByIdQuery(Guid Id) : IRequest<WarehouseDto?>;

public class GetWarehouseByIdQueryHandler(IWarehouseAdminRepository repository)
    : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto?>
{
    public async Task<WarehouseDto?> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var w = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (w is null)
        {
            return null;
        }

        return new WarehouseDto(
            w.Id,
            w.CompanyId,
            w.Name,
            new AddressDto(w.Address.Street, w.Address.City, w.Address.State, w.Address.PostalCode, w.Address.Country),
            w.OperatingHours,
            w.ContactPhone,
            w.ContactEmail,
            w.IsActive
        );
    }
}