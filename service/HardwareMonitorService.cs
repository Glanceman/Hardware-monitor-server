using System.Text.Json;
using HardwareMonitorServer;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Options;

// Background service that updates hardware information
public class HardwareMonitorService : BackgroundService
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor;
    private readonly HardwareDataRepository _repository;
    private readonly WebSocketHandler _webSocketHandler;
    private readonly ILogger<HardwareMonitorService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    
    public HardwareMonitorService(
        Computer computer, 
        UpdateVisitor updateVisitor,
        HardwareDataRepository repository,
        WebSocketHandler webSocketHandler,
        ILogger<HardwareMonitorService> logger)
    {
        _computer = computer;
        _updateVisitor = updateVisitor;
        _repository = repository;
        _webSocketHandler = webSocketHandler;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hardware Monitor Service is starting.");
        
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                UpdateHardwareData();
                await BroadcastHardwareData();
                //sleep
                await Task.Delay(_updateInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hardware Monitor Service is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the Hardware Monitor Service.");
        }
        finally
        {
            _computer.Close();
        }
    }
    
    private void UpdateHardwareData()
    {
        try
        {
            _computer.Accept(_updateVisitor);
            
            var hardwareData = new HardwareData();
            
            foreach (var hardware in _computer.Hardware)
            {
                var hardwareInfo = new HardwareInfo
                {
                    Name = hardware.Name,
                    Sensors = hardware.Sensors.Select(s => new SensorInfo
                    {
                        Name = s.Name,
                        Type = s.SensorType.ToString(),
                        Value = s.Value
                    }).ToList()
                };
                
                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        hardwareData.Cpu.Add(hardwareInfo);
                        break;
                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        hardwareData.Gpu.Add(hardwareInfo);
                        break;
                    case HardwareType.Memory:
                        hardwareData.Memory.Add(hardwareInfo);
                        break;
                    case HardwareType.Storage:
                        hardwareData.Storage.Add(hardwareInfo);
                        break;
                    case HardwareType.Network:
                        hardwareData.Network.Add(hardwareInfo);
                        break;
                }
            }
            
            _repository.UpdateData(hardwareData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hardware data");
        }
    }
    
    private async Task BroadcastHardwareData(bool bShowToConsole = true)
    {
        var data = _repository.GetAllHardwareInfo();
        var options = new JsonSerializerOptions
        {
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        string json = JsonSerializer.Serialize(data, options);
        if(bShowToConsole){
            _logger.LogInformation("Hardware Data: {Json}", json);
        }
        await _webSocketHandler.BroadcastAsync(json);
    }
}