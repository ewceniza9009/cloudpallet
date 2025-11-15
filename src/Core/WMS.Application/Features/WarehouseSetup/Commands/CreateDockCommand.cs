using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record CreateDockCommand(Guid WarehouseId, string Name) : IRequest<Guid>;

public class CreateDockCommandHandler(IWarehouseAdminRepository warehouseAdminRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateDockCommand, Guid>
{
    public async Task<Guid> Handle(CreateDockCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseAdminRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException("Warehouse not found.");

        // This logic belongs in the Warehouse aggregate
        var newDock = warehouse.AddDock(request.Name);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return newDock.Id;
    }
}