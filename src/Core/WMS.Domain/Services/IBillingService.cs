using WMS.Domain.Entities;

namespace WMS.Domain.Services;

public interface IBillingService
{
    Task<Invoice> GenerateInvoiceForAccountAsync(Guid accountId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken);
}