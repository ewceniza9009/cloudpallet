using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Services;             

namespace WMS.Application.Features.Billing.Commands;

public record GenerateInvoiceCommand(Guid AccountId, DateTime PeriodStart, DateTime PeriodEnd) : IRequest<Guid>;

public class GenerateInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IBillingService billingService,
    IUnitOfWork unitOfWork) : IRequestHandler<GenerateInvoiceCommand, Guid>
{
    public async Task<Guid> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await billingService.GenerateInvoiceForAccountAsync(
            request.AccountId,
            request.PeriodStart,
            request.PeriodEnd,
            cancellationToken);

        await invoiceRepository.AddAsync(invoice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}