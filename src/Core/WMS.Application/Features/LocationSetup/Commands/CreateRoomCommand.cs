using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;
using WMS.Domain.Aggregates.Warehouse;
using System.Threading;
using System.Threading.Tasks;

namespace WMS.Application.Features.LocationSetup.Commands;

public record CreateRoomCommand(
    Guid WarehouseId,
    string Name,
    decimal MinTemp,
    decimal MaxTemp,
    ServiceType ServiceType) : IRequest<Guid>;

public class CreateRoomCommandHandler(
    IWarehouseAdminRepository repository,    
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRoomCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var room = Room.Create(
            request.WarehouseId,
            request.Name,
            request.MinTemp,
            request.MaxTemp,
            request.ServiceType
        );

        repository.AddRoom(room);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return room.Id;
    }
}