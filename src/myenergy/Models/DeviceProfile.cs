namespace myenergy.Models;

/// <summary>
/// Represents a configurable household device for smart usage recommendations
/// </summary>
public class DeviceProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "⚡";
    public double PowerKw { get; set; }
    public double DurationHours { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Category { get; set; } = "Other"; // Kitchen, Laundry, Heating, EV, Other
    public bool IsCustom { get; set; } = false; // User-added vs default
    
    // Optional: Flexibility settings
    public bool CanBeDelayed { get; set; } = true;
    public int MaxDelayHours { get; set; } = 12;
    
    // Helper property
    public double EnergyUsedKwh => PowerKw * DurationHours;
}

/// <summary>
/// Device categories for grouping
/// </summary>
public static class DeviceCategories
{
    public const string Kitchen = "Kitchen";
    public const string Laundry = "Laundry";
    public const string Heating = "Heating/Cooling";
    public const string EV = "Electric Vehicle";
    public const string Other = "Other";
    
    public static List<string> All => new() 
    { 
        Kitchen, 
        Laundry, 
        Heating, 
        EV, 
        Other 
    };
}
