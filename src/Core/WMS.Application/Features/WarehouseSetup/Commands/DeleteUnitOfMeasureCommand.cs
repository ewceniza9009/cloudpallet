using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record DeleteUnitOfMeasureCommand(Guid Id) : IRequest;

public class DeleteUnitOfMeasureCommandHandler(IUnitOfMeasureRepository uomRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUnitOfMeasureCommand>
{
    public async Task Handle(DeleteUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var uom = await uomRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"UnitOfMeasure with ID {request.Id} not found.");

        uomRepository.Remove(uom);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}