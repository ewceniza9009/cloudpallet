using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record CreateUnitOfMeasureCommand(
    string Name,
    string Symbol) : IRequest<Guid>;

public class CreateUnitOfMeasureCommandHandler(IUnitOfMeasureRepository uomRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateUnitOfMeasureCommand, Guid>
{
    public async Task<Guid> Handle(CreateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var uom = UnitOfMeasure.Create(request.Name, request.Symbol);

        await uomRepository.AddAsync(uom, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return uom.Id;
    }
}