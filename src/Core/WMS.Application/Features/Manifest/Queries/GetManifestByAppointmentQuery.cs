using AutoMapper;
using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Manifests.Queries;

public class CargoManifestLineDto
{
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal ExpectedQuantity { get; set; }
}

public class CargoManifestDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public List<CargoManifestLineDto> Lines { get; set; } = new();
}

public record GetManifestByAppointmentQuery(Guid AppointmentId) : IRequest<CargoManifestDto?>;

public class GetManifestByAppointmentQueryHandler(
    ICargoManifestRepository manifestRepository,
    IMaterialRepository materialRepository,
    IMapper mapper) : IRequestHandler<GetManifestByAppointmentQuery, CargoManifestDto?>
{
    public async Task<CargoManifestDto?> Handle(GetManifestByAppointmentQuery request, CancellationToken cancellationToken)
    {
        var manifest = await manifestRepository.GetByAppointmentIdAsync(request.AppointmentId, cancellationToken);
        if (manifest is null)
        {
            return null;
        }

        var dto = mapper.Map<CargoManifestDto>(manifest);

        var materialIds = dto.Lines.Select(l => l.MaterialId).ToList();
        var materials = await materialRepository.GetByIdsAsync(materialIds, cancellationToken);
        var materialMap = materials.ToDictionary(m => m.Id, m => m.Name);

        foreach (var line in dto.Lines)
        {
            if (materialMap.TryGetValue(line.MaterialId, out var materialName))
            {
                line.MaterialName = materialName;
            }
        }

        return dto;
    }
}