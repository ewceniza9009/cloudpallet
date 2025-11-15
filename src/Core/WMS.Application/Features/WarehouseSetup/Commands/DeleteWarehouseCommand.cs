using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record DeleteWarehouseCommand(Guid Id) : IRequest;

public class DeleteWarehouseCommandHandler(
    IWarehouseAdminRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteWarehouseCommand>
{
    public async Task Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await repository.GetByIdAsync(request.Id, cancellationToken)
             ?? throw new KeyNotFoundException($"Warehouse with ID {request.Id} not found.");

        repository.Remove(warehouse);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}