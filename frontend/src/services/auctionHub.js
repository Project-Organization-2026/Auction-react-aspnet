import * as signalR from "@microsoft/signalr";

let hubConnection = null;

export function getAuctionHubConnection() {
  if (!hubConnection) {
    const hubUrl =
      import.meta.env.VITE_HUB_URL ||
      (import.meta.env.VITE_API_BASE_URL
        ? import.meta.env.VITE_API_BASE_URL.replace(/\/api\/?$/, "") + "/hubs/auction"
        : "/hubs/auction");

    hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
  return hubConnection;
}

export async function startAuctionHub() {
  const connection = getAuctionHubConnection();
  if (connection.state === signalR.HubConnectionState.Disconnected) {
    try {
      await connection.start();
      console.log("Connected to AuctionHub SignalR");
    } catch (err) {
      console.warn("SignalR connection error (will retry automatically):", err);
    }
  }
  return connection;
}

export async function joinLotGroup(lotId) {
  const connection = await startAuctionHub();
  if (connection.state === signalR.HubConnectionState.Connected) {
    try {
      await connection.invoke("JoinLot", Number(lotId));
    } catch (err) {
      console.warn(`Failed to join group lot-${lotId}:`, err);
    }
  }
}

export async function leaveLotGroup(lotId) {
  const connection = getAuctionHubConnection();
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    try {
      await connection.invoke("LeaveLot", Number(lotId));
    } catch (err) {
      console.warn(`Failed to leave group lot-${lotId}:`, err);
    }
  }
}
