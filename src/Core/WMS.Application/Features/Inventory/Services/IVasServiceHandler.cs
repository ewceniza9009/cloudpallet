using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Application.Features.Inventory.Commands;

namespace WMS.Application.Features.Inventory.Services;

public interface IVasServiceHandler
{
    ServiceType ServiceType { get; }
    Task HandleAsync(RecordVasCommand command, CancellationToken cancellationToken);
}
