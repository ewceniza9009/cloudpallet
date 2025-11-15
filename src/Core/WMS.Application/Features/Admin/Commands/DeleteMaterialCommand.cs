using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Admin.Commands;

public record DeleteMaterialCommand(Guid Id) : IRequest;

public class DeleteMaterialCommandHandler(
    IMaterialRepository materialRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteMaterialCommand>
{
    public async Task Handle(DeleteMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await materialRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Material with ID {request.Id} not found.");

        materialRepository.Remove(material);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}