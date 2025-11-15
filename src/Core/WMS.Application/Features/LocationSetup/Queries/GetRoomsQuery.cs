using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;     
using System.Collections.Generic;    
using System.Linq;      
using System.Threading;    
using System.Threading.Tasks;    

namespace WMS.Application.Features.LocationSetup.Queries;

public record GetRoomsQuery : IRequest<IEnumerable<RoomDto>>;

public class GetRoomsQueryHandler(IWarehouseAdminRepository repository)
    : IRequestHandler<GetRoomsQuery, IEnumerable<RoomDto>>
{
    public async Task<IEnumerable<RoomDto>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        var rooms = await repository.GetAllRoomsAsync(cancellationToken);

        return rooms.Select(r => new RoomDto(
            r.Id,
            r.Name,
            r.ServiceType.ToString(),
            r.TemperatureRange.MinTemperature,
            r.TemperatureRange.MaxTemperature,
            r.Locations.Count
        )).ToList();
    }
}