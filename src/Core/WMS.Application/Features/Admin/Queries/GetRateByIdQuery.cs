using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Admin.Queries;

public record RateDto(
    Guid Id,
    Guid? AccountId,
    ServiceType ServiceType,
    RateUom Uom,
    decimal Value,
    string Tier,
    DateTime EffectiveStartDate,
    DateTime? EffectiveEndDate,
    bool IsActive);

public record GetRateByIdQuery(Guid Id) : IRequest<RateDto?>;

public class GetRateByIdQueryHandler(IRateRepository rateRepository)
    : IRequestHandler<GetRateByIdQuery, RateDto?>
{
    public async Task<RateDto?> Handle(GetRateByIdQuery request, CancellationToken cancellationToken)
    {
        var rate = await rateRepository.GetByIdAsync(request.Id, cancellationToken);

        if (rate is null)
        {
            return null;
        }

        return new RateDto(
            rate.Id,
            rate.AccountId,
            rate.ServiceType,
            rate.Uom,
            rate.Value,
            rate.Tier,
            rate.EffectiveStartDate,
            rate.EffectiveEndDate,
            rate.IsActive
        );
    }
}