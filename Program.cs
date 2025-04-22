using System.Text.Json;
using LibreHardwareMonitor.Hardware;

namespace HardwareMonitorServer
{
    public class Program
    {


        public static void Main(string[] args)
        {
            // Initialize and configure the LibreHardwareMonitor computer object


            // Configure and start the web application
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddSingleton<Computer>(serviceProvider => {
                var computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true,
                    IsNetworkEnabled = true,
                    IsStorageEnabled = true
                };
                computer.Open();
                return computer;
            });

            builder.Services.AddSingleton<UpdateVisitor>();
            builder.Services.AddSingleton<HardwareDataRepository>();
            builder.Services.AddSingleton<WebSocketHandler>();
            builder.Services.AddHostedService<HardwareMonitorService>();

                        // Add CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });






            var app = builder.Build();


            app.UseCors("AllowAll");
            // WebSocket endpoint
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var handler = app.Services.GetRequiredService<WebSocketHandler>();
                        await handler.HandleConnectionAsync(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            // REST API endpoints
            // Default route
            var options = new JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            app.MapGet("/", () => "Hardware Monitor Server - Use /api/hardware for REST API or /ws for WebSocket");
            app.MapGet("/api/hardware", (HardwareDataRepository repo) => JsonSerializer.Serialize(repo.GetAllHardwareInfo(), options));
            app.MapGet("/api/hardware/cpu", (HardwareDataRepository repo) => JsonSerializer.Serialize(repo.GetCpuInfo(), options));
            app.MapGet("/api/hardware/gpu", (HardwareDataRepository repo) => JsonSerializer.Serialize(repo.GetGpuInfo(), options));
            app.MapGet("/api/hardware/memory", (HardwareDataRepository repo) => JsonSerializer.Serialize(repo.GetMemoryInfo(),options));
            app.MapGet("/api/hardware/storage", (HardwareDataRepository repo) => JsonSerializer.Serialize(repo.GetStorageInfo(), options));
            app.MapGet("/api/hardware/network", (HardwareDataRepository repo) => JsonSerializer.Serialize(repo.GetNetworkInfo(), options));

            app.Run();


           
        }
    }
}
