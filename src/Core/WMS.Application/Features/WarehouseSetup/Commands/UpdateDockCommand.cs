using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdateDockCommand(Guid DockId, string Name) : IRequest;

public class UpdateDockCommandHandler(IWarehouseAdminRepository warehouseAdminRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateDockCommand>
{
    public async Task Handle(UpdateDockCommand request, CancellationToken cancellationToken)
    {
        var dock = await warehouseAdminRepository.GetDockByIdAsync(request.DockId, cancellationToken)
            ?? throw new KeyNotFoundException("Dock not found.");

        dock.Update(request.Name); // Assumes Dock.cs has an Update method
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}