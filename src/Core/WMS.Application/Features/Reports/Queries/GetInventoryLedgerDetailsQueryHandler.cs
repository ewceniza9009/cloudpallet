using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Reports.Queries;

namespace WMS.Application.Features.Reports.Queries;

public class GetInventoryLedgerDetailsQueryHandler(IReportRepository reportRepository)
    : IRequestHandler<GetInventoryLedgerDetailsQuery, List<InventoryLedgerLineDto>>
{
    public async Task<List<InventoryLedgerLineDto>> Handle(GetInventoryLedgerDetailsQuery request, CancellationToken cancellationToken)
    {
        return await reportRepository.GetInventoryLedgerDetailsAsync(request, cancellationToken);
    }
}
