using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record DeleteDockCommand(Guid DockId) : IRequest;

public class DeleteDockCommandHandler(IWarehouseAdminRepository warehouseAdminRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteDockCommand>
{
    public async Task Handle(DeleteDockCommand request, CancellationToken cancellationToken)
    {
        var dock = await warehouseAdminRepository.GetDockByIdAsync(request.DockId, cancellationToken)
            ?? throw new KeyNotFoundException("Dock not found.");

        warehouseAdminRepository.RemoveDock(dock);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}