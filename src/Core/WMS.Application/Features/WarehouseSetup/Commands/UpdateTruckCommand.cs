using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdateTruckCommand(
    Guid Id,
    Guid CarrierId,
    string LicensePlate,
    string Model,
    decimal CapacityWeight,
    decimal CapacityVolume,
    bool IsActive) : IRequest;

public class UpdateTruckCommandHandler(
    ITruckRepository truckRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTruckCommand>
{
    public async Task Handle(UpdateTruckCommand request, CancellationToken cancellationToken)
    {
        var truck = await truckRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Truck with ID {request.Id} not found.");

        if (truck.CarrierId != request.CarrierId)
        {
            throw new InvalidOperationException("Changing the carrier of a truck is not allowed.");
        }

        truck.Update(
            request.LicensePlate,
            request.Model,
            request.CapacityWeight,
            request.CapacityVolume,
            request.IsActive);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}