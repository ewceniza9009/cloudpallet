using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record CreateTruckCommand(
    Guid CarrierId,
    string LicensePlate,
    string Model,
    decimal CapacityWeight,
    decimal CapacityVolume) : IRequest<Guid>;

public class CreateTruckCommandHandler(
    ITruckRepository truckRepository,
    ICarrierRepository carrierRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTruckCommand, Guid>
{
    public async Task<Guid> Handle(CreateTruckCommand request, CancellationToken cancellationToken)
    {
        _ = await carrierRepository.GetByIdAsync(request.CarrierId, cancellationToken)
            ?? throw new KeyNotFoundException($"Carrier with ID {request.CarrierId} not found.");

        var truck = Truck.Create(
            request.CarrierId,
            request.LicensePlate,
            request.Model,
            request.CapacityWeight,
            request.CapacityVolume);

        await truckRepository.AddAsync(truck, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return truck.Id;
    }
}