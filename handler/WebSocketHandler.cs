// WebSocket connection handler
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
public class WebSocketHandler
{
    private readonly ILogger<WebSocketHandler> _logger;
    private readonly List<WebSocket> _clients = new();
    private readonly object _lock = new();
    
    public WebSocketHandler(ILogger<WebSocketHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task HandleConnectionAsync(WebSocket webSocket)
    {
        try
        {
            AddClient(webSocket);
            
            _logger.LogInformation("WebSocket client connected");
            
            // Keep the connection open
            var buffer = new byte[1024];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
            
            while (!result.CloseStatus.HasValue)
            {
                // Just keep the connection alive but don't process messages
                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            
            await webSocket.CloseAsync(
                result.CloseStatus.Value,
                result.CloseStatusDescription,
                CancellationToken.None);
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket connection closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket connection");
        }
        finally
        {
            RemoveClient(webSocket);
            _logger.LogInformation("WebSocket client disconnected");
        }
    }
    
    public async Task BroadcastAsync(object data)
    {
        if (!HasClients()) return;
        
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);
        
        List<WebSocket> clientsToRemove = new();
        List<WebSocket> currentClients;
        
        lock (_lock)
        {
            currentClients = _clients.ToList();
        }
        
        foreach (var client in currentClients)
        {
            try
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.SendAsync(
                        buffer,
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                else
                {
                    clientsToRemove.Add(client);
                }
            }
            catch (Exception)
            {
                clientsToRemove.Add(client);
            }
        }
        
        // Remove disconnected clients
        foreach (var client in clientsToRemove)
        {
            RemoveClient(client);
        }
    }
    
    private void AddClient(WebSocket client)
    {
        lock (_lock)
        {
            _clients.Add(client);
        }
    }
    
    private void RemoveClient(WebSocket client)
    {
        lock (_lock)
        {
            _clients.Remove(client);
        }
    }
    
    private bool HasClients()
    {
        lock (_lock)
        {
            return _clients.Count > 0;
        }
    }
}