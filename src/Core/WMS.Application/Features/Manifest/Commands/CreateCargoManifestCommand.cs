using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Manifests.Queries;
using WMS.Domain.Aggregates.Cargo;

namespace WMS.Application.Features.Manifests.Commands;

public record CreateCargoManifestCommand(Guid AppointmentId, List<CargoManifestLineDto> Lines) : IRequest<Guid>;

public class CreateCargoManifestCommandHandler(
    ICargoManifestRepository manifestRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateCargoManifestCommand, Guid>
{
    public async Task<Guid> Handle(CreateCargoManifestCommand request, CancellationToken cancellationToken)
    {
        var manifest = CargoManifest.Create(request.AppointmentId);

        var uniqueMaterials = request.Lines
            .GroupBy(l => l.MaterialId)
            .Select(g => g.First());

        foreach (var line in uniqueMaterials)
        {
            manifest.AddLine(line.MaterialId, line.ExpectedQuantity);
        }

        await manifestRepository.AddAsync(manifest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return manifest.Id;
    }
}