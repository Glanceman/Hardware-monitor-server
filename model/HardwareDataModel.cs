// Data models
public class HardwareData
{
    public List<HardwareInfo> Cpu { get; set; } = new();
    public List<HardwareInfo> Gpu { get; set; } = new();
    public List<HardwareInfo> Memory { get; set; } = new();
    public List<HardwareInfo> Storage { get; set; } = new();
    public List<HardwareInfo> Network { get; set; } = new();
}