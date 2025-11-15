using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs.Interfaces;

namespace WMS.Api.Hubs;

[Authorize]
public class DockHub : Hub<IDockHubClient>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}