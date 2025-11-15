using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Billing.Queries;

public record InvoiceLineDto(Guid Id, string Description, ServiceType ServiceType, decimal Quantity, decimal UnitRate, decimal Amount);
public record InvoiceDetailDto(
    Guid Id,
    Guid AccountId,
    string Name, // ADDED
    string InvoiceNumber,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime DueDate,
    decimal TotalAmount,
    decimal TaxAmount,
    InvoiceStatus Status,
    List<InvoiceLineDto> Lines);

public record GetInvoiceByIdQuery(Guid InvoiceId) : IRequest<InvoiceDetailDto?>;

public class GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepository) : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto?>
{
    public async Task<InvoiceDetailDto?> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdWithLinesAsync(request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        return new InvoiceDetailDto(
            invoice.Id,
            invoice.AccountId,
            invoice.Account.Name,
            invoice.InvoiceNumber,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.DueDate,
            invoice.TotalAmount,
            invoice.TaxAmount,
            invoice.Status,
            invoice.Lines.Select(l => new InvoiceLineDto(l.Id, l.Description, l.ServiceType, l.Quantity, l.UnitRate, l.Amount)).ToList()
        );
    }
}