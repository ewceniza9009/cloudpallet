using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Admin.Queries;

public record GetRatesQuery : IRequest<IEnumerable<RateDto>>;

public class GetRatesQueryHandler(IRateRepository rateRepository)
    : IRequestHandler<GetRatesQuery, IEnumerable<RateDto>>
{
    public async Task<IEnumerable<RateDto>> Handle(GetRatesQuery request, CancellationToken cancellationToken)
    {
        var rates = await rateRepository.GetAllAsync(cancellationToken);

        return rates.Select(rate => new RateDto(
            rate.Id,
            rate.AccountId,
            rate.ServiceType,
            rate.Uom,
            rate.Value,
            rate.Tier,
            rate.EffectiveStartDate,
            rate.EffectiveEndDate,
            rate.IsActive
        ));
    }
}