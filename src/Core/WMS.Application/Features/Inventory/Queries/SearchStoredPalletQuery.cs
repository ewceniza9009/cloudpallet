using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;

namespace WMS.Application.Features.Inventory.Queries;

/// <summary>
/// DTO for returning pallet search results.
/// </summary>
public record StoredPalletSearchResultDto
{
    public Guid PalletId { get; init; }
    public string PalletBarcode { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string MaterialSummary { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
}

/// <summary>
/// Query to find stored pallets based on various optional criteria.
/// </summary>
public record SearchStoredPalletsQuery(
    Guid WarehouseId,
    Guid? AccountId,
    Guid? MaterialId,
    string? BarcodeQuery // Can be LPN or Pallet SSCC
    ) : IRequest<IEnumerable<StoredPalletSearchResultDto>>;

public class SearchStoredPalletsQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<SearchStoredPalletsQuery, IEnumerable<StoredPalletSearchResultDto>>
{
    public async Task<IEnumerable<StoredPalletSearchResultDto>> Handle(SearchStoredPalletsQuery request, CancellationToken cancellationToken)
    {
        return await warehouseRepository.SearchStoredPalletsAsync(
            request.WarehouseId,
            request.AccountId,
            request.MaterialId,
            request.BarcodeQuery,
            cancellationToken);
    }
}