using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record DeleteSupplierCommand(Guid Id) : IRequest;

public class DeleteSupplierCommandHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteSupplierCommand>
{
    public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier with ID {request.Id} not found.");

        // Add check here: e.g., if (await receivingRepository.IsSupplierInUse(request.Id))
        //    throw new InvalidOperationException("Cannot delete supplier with existing receiving history.");

        supplierRepository.Remove(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}