// src/Core/WMS.Application/Features/Inventory/Queries/GetReceivingSessionsQuery.cs
using MediatR;
using WMS.Application.Abstractions.Persistence;
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

public record GetReceivingSessionsQuery(Guid WarehouseId) : IRequest<IEnumerable<ReceivingSessionDto>>;

public class GetReceivingSessionsQueryHandler(IReceivingTransactionRepository receivingRepository)
    : IRequestHandler<GetReceivingSessionsQuery, IEnumerable<ReceivingSessionDto>>
{
    public async Task<IEnumerable<ReceivingSessionDto>> Handle(GetReceivingSessionsQuery request, CancellationToken cancellationToken)
    {
        return await receivingRepository.GetReceivingSessionsByWarehouseAsync(request.WarehouseId, cancellationToken);
    }
}