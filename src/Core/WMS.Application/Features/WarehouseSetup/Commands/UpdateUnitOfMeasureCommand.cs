using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdateUnitOfMeasureCommand(
    Guid Id,
    string Name,
    string Symbol,
    bool IsActive) : IRequest;

public class UpdateUnitOfMeasureCommandHandler(IUnitOfMeasureRepository uomRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUnitOfMeasureCommand>
{
    public async Task Handle(UpdateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var uom = await uomRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"UnitOfMeasure with ID {request.Id} not found.");

        uom.Update(request.Name, request.Symbol, request.IsActive);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}