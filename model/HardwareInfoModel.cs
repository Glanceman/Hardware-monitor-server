using LibreHardwareMonitor.Hardware;
public class HardwareInfo
{
    public string Name { get; set; } = string.Empty;
    public List<SensorInfo> Sensors { get; set; } = new();
}