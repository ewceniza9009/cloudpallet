using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdatePalletTypeCommand(
    Guid Id,
    string Name,
    decimal TareWeight,
    decimal Length,
    decimal Width,
    decimal Height,
    bool IsActive) : IRequest;

public class UpdatePalletTypeCommandHandler(
    IPalletTypeRepository palletTypeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePalletTypeCommand>
{
    public async Task Handle(UpdatePalletTypeCommand request, CancellationToken cancellationToken)
    {
        var palletType = await palletTypeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"PalletType with ID {request.Id} not found.");

        palletType.Update(
            request.Name,
            request.TareWeight,
            request.Length,
            request.Width,
            request.Height,
            request.IsActive);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}