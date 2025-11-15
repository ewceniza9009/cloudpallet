using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.LocationSetup.Queries;

public record GetRoomDetailsQuery(Guid Id) : IRequest<RoomDetailDto?>;

public class GetRoomDetailsQueryHandler(IWarehouseAdminRepository repository)
    : IRequestHandler<GetRoomDetailsQuery, RoomDetailDto?>
{
    public async Task<RoomDetailDto?> Handle(GetRoomDetailsQuery request, CancellationToken cancellationToken)
    {
        var room = await repository.GetRoomByIdAsync(request.Id, cancellationToken);    
        if (room is null) return null;

        return new RoomDetailDto(
            room.Id,
            room.WarehouseId,
            room.Name,
            room.ServiceType.ToString(),
            room.TemperatureRange.MinTemperature,
            room.TemperatureRange.MaxTemperature,
            room.IsActive
        );
    }
}