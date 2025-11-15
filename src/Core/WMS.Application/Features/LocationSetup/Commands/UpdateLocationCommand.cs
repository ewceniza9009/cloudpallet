using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.LocationSetup.Commands;

public record UpdateLocationCommand(
    Guid LocationId,
    LocationType ZoneType,
    decimal CapacityWeight,
    bool IsActive) : IRequest;

public class UpdateLocationCommandHandler(
    IWarehouseAdminRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLocationCommand>
{
    public async Task Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await repository.GetLocationByIdAsync(request.LocationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Location with ID {request.LocationId} not found.");

        var weight = Weight.Create(request.CapacityWeight, "KG");   

        location.Update(
            request.ZoneType,
            weight,
            request.IsActive
        );

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}