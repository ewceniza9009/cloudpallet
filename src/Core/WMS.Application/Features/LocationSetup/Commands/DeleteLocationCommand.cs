using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.LocationSetup.Commands;

public record DeleteLocationCommand(Guid LocationId) : IRequest;

public class DeleteLocationCommandHandler(
    IWarehouseAdminRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteLocationCommand>
{
    public async Task Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await repository.GetLocationByIdAsync(request.LocationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Location with ID {request.LocationId} not found.");

        if (!location.IsEmpty)
        {
            throw new InvalidOperationException("Cannot delete a location that is not empty.");
        }

        repository.RemoveLocation(location);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}