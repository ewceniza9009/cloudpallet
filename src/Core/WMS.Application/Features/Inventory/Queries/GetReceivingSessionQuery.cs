using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Queries;

public record ReceivingSessionDto
{
    public Guid ReceivingId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string? LicensePlate { get; init; }
    public ReceivingStatus Status { get; init; }
    public DateTime Timestamp { get; init; }
    public int PalletCount { get; init; }
}

public record GetReceivingSessionsQuery : IRequest<PagedResult<ReceivingSessionDto>>
{
    public Guid WarehouseId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetReceivingSessionsQueryHandler(IReceivingTransactionRepository receivingRepository)
    : IRequestHandler<GetReceivingSessionsQuery, PagedResult<ReceivingSessionDto>>
{
    public async Task<PagedResult<ReceivingSessionDto>> Handle(GetReceivingSessionsQuery request, CancellationToken cancellationToken)
    {
        return await receivingRepository.GetReceivingSessionsByWarehouseAsync(request, cancellationToken);
    }
}