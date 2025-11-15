using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;         

namespace WMS.Application.Features.LocationSetup.Queries;

public record GetLocationsForRoomQuery(Guid RoomId) : IRequest<PagedResult<LocationDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public string? SearchTerm { get; init; }
}

public class GetLocationsForRoomQueryHandler(IWarehouseAdminRepository repository)
    : IRequestHandler<GetLocationsForRoomQuery, PagedResult<LocationDto>>
{
    public async Task<PagedResult<LocationDto>> Handle(GetLocationsForRoomQuery request, CancellationToken cancellationToken)
    {
        var pagedResult_Domain = await repository.GetLocationsForRoomAsync(request, cancellationToken);

        var dtoList = pagedResult_Domain.Items.Select(l => new LocationDto(
            l.Id,
            l.Barcode,
            l.Bay,
            l.Row,
            l.Column,
            l.Level,
            l.ZoneType.ToString(),
            l.CapacityWeight.Value,
            l.IsEmpty,
            l.IsActive
        )).ToList();

        return new PagedResult<LocationDto>
        {
            Items = dtoList,     
            TotalCount = pagedResult_Domain.TotalCount    
        };
    }
}