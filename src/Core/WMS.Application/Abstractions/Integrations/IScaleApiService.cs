using WMS.Domain.ValueObjects;

namespace WMS.Application.Abstractions.Integrations;

public interface IScaleApiService
{
    Task<Weight> GetCurrentWeightAsync(CancellationToken cancellationToken);
}