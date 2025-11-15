using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.LocationSetup.Commands;

public record CreateSimpleLocationCommand(
    Guid RoomId,
    string Name,          
    LocationType ZoneType) : IRequest<Guid>;

public class CreateSimpleLocationCommandHandler(
    IWarehouseAdminRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSimpleLocationCommand, Guid>
{
    public async Task<Guid> Handle(CreateSimpleLocationCommand request, CancellationToken cancellationToken)
    {
        var room = await repository.GetRoomByIdAsync(request.RoomId, cancellationToken)
            ?? throw new KeyNotFoundException($"Room with ID {request.RoomId} not found.");

        var newLocation = room.AddSimpleLocation(request.Name, request.ZoneType);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newLocation.Id;
    }
}