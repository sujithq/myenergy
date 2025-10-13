# Device Configuration Feature for Smart Usage Advisor

## Overview
Made the Smart Usage Advisor's device recommendations fully configurable. Users can now add, edit, enable/disable, and customize their household devices for personalized energy usage recommendations.

## New Files Created

### 1. `Models/DeviceProfile.cs`
- **DeviceProfile class**: Represents a configurable household device
  - Properties:
    - `Id`: Unique identifier
    - `Name`: Device name (e.g., "Dishwasher")
    - `Icon`: Emoji icon for visual representation
    - `PowerKw`: Power consumption in kilowatts
    - `DurationHours`: Typical runtime duration
    - `IsEnabled`: Whether device is included in recommendations
    - `Category`: Device grouping (Kitchen, Laundry, Heating/Cooling, EV, Other)
    - `IsCustom`: Distinguishes user-added vs default devices
    - `CanBeDelayed`: Whether usage can be postponed
    - `MaxDelayHours`: Maximum delay tolerance
    - `EnergyUsedKwh`: Calculated property (PowerKw × DurationHours)

- **DeviceCategories static class**: Defines standard categories

### 2. `Services/DeviceProfileService.cs`
- **Purpose**: Manages device profiles with localStorage persistence
- **Key Methods**:
  - `InitializeAsync()`: Loads from localStorage or creates defaults
  - `GetAllDevices()`: Returns all configured devices
  - `GetEnabledDevices()`: Returns only enabled devices
  - `AddDeviceAsync(device)`: Adds custom device
  - `UpdateDeviceAsync(device)`: Updates existing device
  - `DeleteDeviceAsync(id)`: Removes custom devices only
  - `ToggleDeviceAsync(id)`: Enable/disable device
  - `ResetToDefaultsAsync()`: Restores factory defaults

- **Default Devices** (6 pre-configured):
  1. Dishwasher (1.5 kW, 3h) - Kitchen
  2. Washing Machine (2.0 kW, 1.5h) - Laundry
  3. Tumble Dryer (3.0 kW, 1h) - Laundry
  4. EV Charger (11.0 kW, 4h) - Electric Vehicle
  5. Induction Cooktop (3.5 kW, 0.5h) - Kitchen
  6. Electric Oven (2.5 kW, 1h) - Kitchen

## Modified Files

### 1. `Program.cs`
- Registered `DeviceProfileService` as scoped service
- Now available for dependency injection throughout the app

### 2. `Pages/SmartUsageAdvisor.razor`
#### UI Enhancements:
- **Device Management Panel** (collapsible):
  - Table showing all devices with their properties
  - Enable/disable toggle switches
  - Edit and delete buttons for each device
  - Category badges and custom device indicators
  - "Add Device" button
  - "Reset to Defaults" button with confirmation

- **Add/Edit Device Modal**:
  - Name input
  - Icon emoji selector
  - Category dropdown
  - Power consumption (kW) input
  - Duration (hours) input
  - Live energy calculation display
  - Enable/disable toggle
  - Delay configuration (can be delayed, max delay hours)
  - Save/Cancel buttons

#### Code Changes:
- **New Properties**:
  - `showDeviceManagement`: Toggle device panel visibility
  - `showDeviceModal`: Control modal display
  - `editingDevice`: Holds device being added/edited

- **Modified Methods**:
  - `OnInitializedAsync()`: Now initializes DeviceProfileService
  - `GenerateDeviceRecommendations()`: Uses DeviceService instead of hardcoded list

- **New Methods**:
  - `ToggleDeviceManagement()`: Show/hide device panel
  - `ShowAddDeviceModal()`: Open modal for new device
  - `ShowEditDeviceModal(device)`: Open modal with existing device
  - `CloseDeviceModal()`: Close modal and clear state
  - `SaveDevice()`: Save new or updated device
  - `ToggleDevice(id)`: Enable/disable device
  - `DeleteDevice(id)`: Remove custom device with confirmation
  - `ResetToDefaults()`: Restore factory devices with confirmation

## User Features

### 1. View All Devices
- Expandable device management section
- Shows: enabled status, name, icon, category, power, duration, energy usage, delay capability
- Visual indicators: custom badge, category badge
- Count of enabled devices in header

### 2. Add Custom Devices
- Click "Add Device" button
- Configure all properties
- Automatically marked as custom
- Persists to localStorage

### 3. Edit Devices
- Edit button for each device (default or custom)
- Modify any property
- Changes save immediately

### 4. Enable/Disable Devices
- Quick toggle switches
- Disabled devices excluded from recommendations
- Visual feedback (muted text)

### 5. Delete Custom Devices
- Delete button only for custom devices
- Confirmation dialog prevents accidents
- Default devices cannot be deleted

### 6. Reset to Defaults
- "Reset to Defaults" button
- Confirmation dialog
- Removes all custom devices
- Restores 6 factory defaults

## Data Persistence

- **Storage**: Browser localStorage
- **Key**: `myenergy_device_profiles`
- **Format**: JSON serialized list
- **Lifetime**: Persistent across sessions
- **Scope**: Per-browser/per-user

## Benefits

1. **Personalization**: Users configure their actual household devices
2. **Flexibility**: Add devices not in defaults (pool pumps, heat pumps, etc.)
3. **Accuracy**: Realistic power consumption and duration values
4. **Control**: Enable only relevant devices
5. **Privacy**: All data stored locally
6. **Simplicity**: No server-side configuration needed

## Usage Example

```csharp
// Add a heat pump
var heatPump = new DeviceProfile
{
    Name = "Heat Pump",
    Icon = "🌡️",
    PowerKw = 2.5,
    DurationHours = 8.0,
    Category = DeviceCategories.Heating,
    IsEnabled = true,
    CanBeDelayed = true,
    MaxDelayHours = 2
};
await DeviceService.AddDeviceAsync(heatPump);
```

## Technical Notes

### Category System
- Predefined categories for grouping
- Dropdown selection in UI
- Easy to extend with more categories

### Energy Calculation
- Automatic: `EnergyUsedKwh = PowerKw × DurationHours`
- Displayed in real-time in modal
- Used for cost estimation in recommendations

### Default vs Custom
- `IsCustom` flag distinguishes device origin
- Default devices: cannot be deleted, can be edited
- Custom devices: full control (edit, delete, enable/disable)

### Validation
- Name required (checked in SaveDevice)
- Numeric fields have step values
- Icon limited to 4 characters (supports multi-byte emojis)

## Future Enhancements

Potential improvements:
- [ ] Import/export device profiles (JSON)
- [ ] Device templates/presets
- [ ] Usage history per device
- [ ] Cost tracking per device
- [ ] Device groups/rooms
- [ ] Smart scheduling (automatically delay until optimal time)
- [ ] Device priority levels
- [ ] Seasonal profiles (winter vs summer usage)

## Testing Checklist

- [x] Add new custom device
- [x] Edit existing device (default and custom)
- [x] Enable/disable devices
- [x] Delete custom device (with confirmation)
- [x] Reset to defaults (with confirmation)
- [x] Verify localStorage persistence
- [x] Check recommendations update after changes
- [x] Validate modal UI (add and edit modes)
- [x] Test category dropdown
- [x] Verify energy calculation display

## Files Summary

**Created:**
- `Models/DeviceProfile.cs` (51 lines)
- `Services/DeviceProfileService.cs` (177 lines)

**Modified:**
- `Program.cs` (added service registration)
- `Pages/SmartUsageAdvisor.razor` (added 200+ lines for device management UI and logic)

**Total New Code:** ~430 lines
**Impact:** Major feature - fully configurable device system
