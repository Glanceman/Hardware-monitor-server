// Repository that holds the hardware data
public class HardwareDataRepository
{
    private HardwareData _currentData = new();
    private readonly object _lock = new();
    
    public void UpdateData(HardwareData newData)
    {
        lock (_lock)
        {
            _currentData = newData;
        }
    }
    
    public HardwareData GetAllHardwareInfo()
    {
        lock (_lock)
        {
            return _currentData;
        }
    }
    
    public List<HardwareInfo> GetCpuInfo()
    {
        lock (_lock)
        {
            return _currentData.Cpu;
        }
    }
    
    public List<HardwareInfo> GetGpuInfo()
    {
        lock (_lock)
        {
            return _currentData.Gpu;
        }
    }
    
    public List<HardwareInfo> GetMemoryInfo()
    {
        lock (_lock)
        {
            return _currentData.Memory;
        }
    }
    
    public List<HardwareInfo> GetStorageInfo()
    {
        lock (_lock)
        {
            return _currentData.Storage;
        }
    }
    
    public List<HardwareInfo> GetNetworkInfo()
    {
        lock (_lock)
        {
            return _currentData.Network;
        }
    }
}