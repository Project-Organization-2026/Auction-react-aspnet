using Microsoft.AspNetCore.SignalR;

namespace Auction.API.Hubs;

public class AuctionHub : Hub
{
    public async Task JoinLot(int lotId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"lot-{lotId}");
    }

    public async Task LeaveLot(int lotId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lot-{lotId}");
    }
}
