using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(WmsDbContext context) : IInvoiceRepository
{
    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        context.Invoices.Add(invoice);
        return Task.CompletedTask;
    }

    public async Task<Invoice?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Invoices
            .Include(i => i.Account)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetForAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return await context.Invoices
            .AsNoTracking()
            .Where(i => i.AccountId == accountId)
            .OrderByDescending(i => i.PeriodEnd)
            .ToListAsync(cancellationToken);
    }
}