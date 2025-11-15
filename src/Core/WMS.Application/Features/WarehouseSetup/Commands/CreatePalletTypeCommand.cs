using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record CreatePalletTypeCommand(
    string Name,
    decimal TareWeight,
    decimal Length,
    decimal Width,
    decimal Height) : IRequest<Guid>;

public class CreatePalletTypeCommandHandler(
    IPalletTypeRepository palletTypeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePalletTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreatePalletTypeCommand request, CancellationToken cancellationToken)
    {
        var palletType = PalletType.Create(
            request.Name,
            request.TareWeight,
            request.Length,
            request.Width,
            request.Height);

        await palletTypeRepository.AddAsync(palletType, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return palletType.Id;
    }
}