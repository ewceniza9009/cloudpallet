using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record DeleteTruckCommand(Guid Id) : IRequest;

public class DeleteTruckCommandHandler(
    ITruckRepository truckRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTruckCommand>
{
    public async Task Handle(DeleteTruckCommand request, CancellationToken cancellationToken)
    {
        var truck = await truckRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Truck with ID {request.Id} not found.");

        // Add any business logic here (e.g., check if truck is on-site)
        // if (truck.IsOnSite) 
        // {
        //    throw new InvalidOperationException("Cannot delete a truck that is currently on-site.");
        // }

        truckRepository.Remove(truck);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}