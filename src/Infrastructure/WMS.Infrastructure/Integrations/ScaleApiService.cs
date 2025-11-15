using WMS.Application.Abstractions.Integrations;
using WMS.Domain.ValueObjects;

namespace WMS.Infrastructure.Integrations;

public class ScaleApiService(IHttpClientFactory httpClientFactory) : IScaleApiService
{
    //private readonly HttpClient _httpClient = httpClientFactory.CreateClient("ScaleApi");

    public async Task<Weight> GetCurrentWeightAsync(CancellationToken cancellationToken)
    {
        // Make an HTTP call to the scale's API.
        // var response = await _httpClient.GetFromJsonAsync<ScaleApiResponse>("/weight", cancellationToken);
        // return Weight.Create(response!.Weight, response.Unit);

        // For this project, we simulate the scale reading.
        await Task.Delay(150, cancellationToken); // Simulate network latency
        var randomWeight = new Random().Next(250, 18000) / 10.0m; // Simulate pallet weight between 25kg and 1800kg
        return Weight.Create(Math.Round(randomWeight, 2), "KG");
    }
}

file record ScaleApiResponse(decimal Weight, string Unit);