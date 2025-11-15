using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);
    Task<Invoice?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Invoice>> GetForAccountAsync(Guid accountId, CancellationToken cancellationToken);   
}