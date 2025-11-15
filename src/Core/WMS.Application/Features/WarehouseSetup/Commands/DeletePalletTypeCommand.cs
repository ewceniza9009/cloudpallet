using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record DeletePalletTypeCommand(Guid Id) : IRequest;

public class DeletePalletTypeCommandHandler(
    IPalletTypeRepository palletTypeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePalletTypeCommand>
{
    public async Task Handle(DeletePalletTypeCommand request, CancellationToken cancellationToken)
    {
        var palletType = await palletTypeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"PalletType with ID {request.Id} not found.");

        palletTypeRepository.Remove(palletType);    
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}