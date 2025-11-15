using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Billing.Queries;

public record InvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public DateTime DueDate { get; init; }
    public decimal TotalAmount { get; init; }
    public InvoiceStatus Status { get; init; }
}

public record GetInvoicesByAccountQuery(Guid AccountId) : IRequest<IEnumerable<InvoiceDto>>;

public class GetInvoicesByAccountQueryHandler(IInvoiceRepository invoiceRepository) : IRequestHandler<GetInvoicesByAccountQuery, IEnumerable<InvoiceDto>>
{
    public async Task<IEnumerable<InvoiceDto>> Handle(GetInvoicesByAccountQuery request, CancellationToken cancellationToken)
    {
        var invoices = await invoiceRepository.GetForAccountAsync(request.AccountId, cancellationToken);

        return invoices.Select(i => new InvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            PeriodStart = i.PeriodStart,
            PeriodEnd = i.PeriodEnd,
            DueDate = i.DueDate,
            TotalAmount = i.TotalAmount,
            Status = i.Status
        });
    }
}