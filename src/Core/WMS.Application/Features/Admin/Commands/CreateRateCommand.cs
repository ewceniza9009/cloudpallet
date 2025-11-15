// src/Core/WMS.Application/Features/Admin/Commands/CreateRateCommand.cs (New File)
using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Admin.Commands;

public record CreateRateCommand(
    Guid? AccountId,
    ServiceType ServiceType,
    RateUom Uom,
    decimal Value,
    string Tier,
    DateTime EffectiveStartDate,
    DateTime? EffectiveEndDate) : IRequest<Guid>;

public class CreateRateCommandHandler(IRateRepository rateRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateRateCommand, Guid>
{
    public async Task<Guid> Handle(CreateRateCommand request, CancellationToken cancellationToken)
    {
        var rate = Rate.Create(
            request.AccountId,
            request.ServiceType,
            request.Uom,
            request.Value,
            request.Tier,
            request.EffectiveStartDate,
            request.EffectiveEndDate);

        await rateRepository.AddAsync(rate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return rate.Id;
    }
}