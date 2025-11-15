// ---- File: src/Core/WMS.Application/Services/PutawaySuggestionService.cs ----

using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums; // <-- Add this
using WMS.Domain.Services;

namespace WMS.Application.Services;

public class PutawaySuggestionService(
    IMaterialRepository materialRepository,
    ILocationRepository locationRepository)
    : IPutawaySuggestionService
{
    public async Task<Guid> SuggestLocationAsync(Guid materialId, CancellationToken cancellationToken)
    {
        var material = await materialRepository.GetByIdAsync(materialId, cancellationToken)
            ?? throw new InvalidOperationException($"Material with ID {materialId} not found.");

        // --- START MODIFICATION ---
        // 1. Get the material's required temp zone
        var requiredZone = material.RequiredTempZone;

        // 2. Map the TempZone enum to the corresponding Room's ServiceType enum
        var requiredServiceType = requiredZone switch
        {
            TempZone.FrozenStorage => ServiceType.FrozenStorage,
            TempZone.Chilling => ServiceType.Chilling,
            TempZone.CoolStorage => ServiceType.CoolStorage,
            TempZone.DeepFrozenStorage => ServiceType.DeepFrozenStorage,
            TempZone.ULTStorage => ServiceType.ULTStorage,
            _ => throw new InvalidOperationException($"No storage service type mapping exists for TempZone '{requiredZone}'.")
        };

        // 3. Find a location in a room with that *ServiceType*
        var bestLocation = await locationRepository.FindBestAvailableLocationAsync(requiredServiceType, cancellationToken)
            ?? throw new InvalidOperationException($"No available storage locations found for type: {requiredServiceType}");

        return bestLocation.Id;
        // --- END MODIFICATION ---
    }
}