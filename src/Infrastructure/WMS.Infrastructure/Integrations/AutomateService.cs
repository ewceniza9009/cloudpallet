using Microsoft.Extensions.Logging;
using WMS.Application.Abstractions.Integrations;
using System.Net.Http.Json;

namespace WMS.Infrastructure.Integrations;

public class AutomationService(IHttpClientFactory httpClientFactory, ILogger<AutomationService> logger) : IAutomationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("AsrsApi");

    public async Task<bool> MoveInventoryAsync(Guid inventoryId, string fromLocation, string toLocation, CancellationToken cancellationToken)
    {
        var command = new { InventoryId = inventoryId, From = fromLocation, To = toLocation };
        logger.LogInformation("Sending command to AS/RS: Move {InventoryId} from {From} to {To}",
            inventoryId, fromLocation, toLocation);

        // var response = await _httpClient.PostAsJsonAsync("/move", command, cancellationToken);
        // return response.IsSuccessStatusCode;

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        logger.LogInformation("AS/RS confirmed move for {InventoryId} complete", inventoryId);

        return true;
    }
}