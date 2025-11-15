using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs.Interfaces;

namespace WMS.Api.Hubs;

[Authorize]
public class TemperatureHub : Hub<ITemperatureHubClient>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}