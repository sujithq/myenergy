using System.Text.Json;
using Microsoft.JSInterop;
using myenergy.Models;

namespace myenergy.Services;

/// <summary>
/// Manages device profiles for smart usage recommendations
/// </summary>
public class DeviceProfileService
{
    private const string STORAGE_KEY = "myenergy_device_profiles";
    private List<DeviceProfile> _devices = new();
    private readonly IJSRuntime _jsRuntime;

    public DeviceProfileService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Try to load from localStorage
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY);
            
            if (!string.IsNullOrEmpty(json))
            {
                var stored = JsonSerializer.Deserialize<List<DeviceProfile>>(json);
                if (stored != null && stored.Any())
                {
                    _devices = stored;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading device profiles: {ex.Message}");
        }

        // Initialize with defaults if nothing was loaded
        InitializeDefaultDevices();
        await SaveAsync();
    }

    private void InitializeDefaultDevices()
    {
        _devices = new List<DeviceProfile>
        {
            new DeviceProfile
            {
                Name = "Dishwasher",
                Icon = "🍽️",
                PowerKw = 1.5,
                DurationHours = 3.0,
                Category = DeviceCategories.Kitchen,
                IsEnabled = true,
                CanBeDelayed = true,
                MaxDelayHours = 12
            },
            new DeviceProfile
            {
                Name = "Washing Machine",
                Icon = "🧺",
                PowerKw = 2.0,
                DurationHours = 1.5,
                Category = DeviceCategories.Laundry,
                IsEnabled = true,
                CanBeDelayed = true,
                MaxDelayHours = 12
            },
            new DeviceProfile
            {
                Name = "Tumble Dryer",
                Icon = "👕",
                PowerKw = 3.0,
                DurationHours = 1.0,
                Category = DeviceCategories.Laundry,
                IsEnabled = true,
                CanBeDelayed = true,
                MaxDelayHours = 12
            },
            new DeviceProfile
            {
                Name = "EV Charger",
                Icon = "🚗",
                PowerKw = 11.0,
                DurationHours = 4.0,
                Category = DeviceCategories.EV,
                IsEnabled = true,
                CanBeDelayed = true,
                MaxDelayHours = 8
            },
            new DeviceProfile
            {
                Name = "Induction Cooktop",
                Icon = "🍳",
                PowerKw = 3.5,
                DurationHours = 0.5,
                Category = DeviceCategories.Kitchen,
                IsEnabled = true,
                CanBeDelayed = false,
                MaxDelayHours = 2
            },
            new DeviceProfile
            {
                Name = "Electric Oven",
                Icon = "🍕",
                PowerKw = 2.5,
                DurationHours = 1.0,
                Category = DeviceCategories.Kitchen,
                IsEnabled = true,
                CanBeDelayed = true,
                MaxDelayHours = 4
            }
        };
    }

    public List<DeviceProfile> GetAllDevices()
    {
        return _devices.ToList();
    }

    public List<DeviceProfile> GetEnabledDevices()
    {
        return _devices.Where(d => d.IsEnabled).ToList();
    }

    public DeviceProfile? GetDevice(string id)
    {
        return _devices.FirstOrDefault(d => d.Id == id);
    }

    public async Task AddDeviceAsync(DeviceProfile device)
    {
        device.Id = Guid.NewGuid().ToString();
        device.IsCustom = true;
        _devices.Add(device);
        await SaveAsync();
    }

    public async Task UpdateDeviceAsync(DeviceProfile device)
    {
        var existing = _devices.FirstOrDefault(d => d.Id == device.Id);
        if (existing != null)
        {
            var index = _devices.IndexOf(existing);
            _devices[index] = device;
            await SaveAsync();
        }
    }

    public async Task DeleteDeviceAsync(string id)
    {
        var device = _devices.FirstOrDefault(d => d.Id == id);
        if (device != null && device.IsCustom)
        {
            _devices.Remove(device);
            await SaveAsync();
        }
    }

    public async Task ToggleDeviceAsync(string id)
    {
        var device = _devices.FirstOrDefault(d => d.Id == id);
        if (device != null)
        {
            device.IsEnabled = !device.IsEnabled;
            await SaveAsync();
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        InitializeDefaultDevices();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_devices);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving device profiles: {ex.Message}");
        }
    }
}
