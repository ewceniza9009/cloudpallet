using MediatR;
using WMS.Application.Features.Reports.Queries;

namespace WMS.Application.Features.Reports.Queries;

public record GetInventoryLedgerDetailsQuery : IRequest<List<InventoryLedgerLineDto>>
{
    public Guid MaterialId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? TruckId { get; init; }
    public Guid? UserId { get; init; }
}
