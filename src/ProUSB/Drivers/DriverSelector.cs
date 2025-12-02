namespace ProUSB.Drivers;

public class DriverSelector
{
    private readonly List<IDriverAdapter> _drivers;

    public DriverSelector(IEnumerable<IDriverAdapter> drivers)
    {
        _drivers = drivers.ToList();
    }

    public IDriverAdapter SelectBestDriver()
    {
        var available = _drivers.Where(d => d.IsAvailable).ToList();
        
        if (available.Count == 0)
            throw new InvalidOperationException("No disk drivers are available on this system");

        var native = available.FirstOrDefault(d => d.Name.Contains("Native", StringComparison.OrdinalIgnoreCase));
        if (native != null) return native;

        return available[0];
    }

    public IEnumerable<IDriverAdapter> GetAvailableDrivers()
    {
        return _drivers.Where(d => d.IsAvailable);
    }
}
