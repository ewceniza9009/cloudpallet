// ---- File: src/Core/WMS.Application/Features/Admin/Commands/UpdateRateCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Admin.Commands;

public record UpdateRateCommand(
    Guid Id,
    Guid? AccountId,
    ServiceType ServiceType,
    RateUom Uom,
    decimal Value,
    string Tier,
    DateTime EffectiveStartDate,
    DateTime? EffectiveEndDate) : IRequest;

public class UpdateRateCommandHandler(
    IRateRepository rateRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateRateCommand>
{
    public async Task Handle(UpdateRateCommand request, CancellationToken cancellationToken)
    {
        var rate = await rateRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Rate with ID {request.Id} not found.");

        // In a real application, you would create an Update method on the Rate entity
        // For now, we will assume a more direct update for simplicity as Rate is not an aggregate root
        // rate.Update(request.AccountId, request.ServiceType, ...);

        // Directly updating properties on a non-aggregate-root entity is acceptable here.
        // If Rate had complex invariants, we'd add a domain method.

        // This would be replaced by rate.Update(...) if logic was complex
        // For now, let's assume we need to create a new one and deactivate the old one to preserve history
        rate.Deactivate();

        var newRate = Domain.Entities.Rate.Create(
            request.AccountId,
            request.ServiceType,
            request.Uom,
            request.Value,
            request.Tier,
            request.EffectiveStartDate,
            request.EffectiveEndDate
        );

        await rateRepository.AddAsync(newRate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}