using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Yard.Commands;

public class VacateYardSpotCommandHandler : IRequestHandler<VacateYardSpotCommand, Unit>
{
    private readonly IWarehouseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public VacateYardSpotCommandHandler(IWarehouseRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(VacateYardSpotCommand request, CancellationToken cancellationToken)
    {
        var yardSpot = await _repository.GetYardSpotByIdAsync(request.YardSpotId, cancellationToken);
        if (yardSpot is null)
        {
            throw new KeyNotFoundException($"Yard spot with ID {request.YardSpotId} not found.");
        }

        yardSpot.Vacate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
