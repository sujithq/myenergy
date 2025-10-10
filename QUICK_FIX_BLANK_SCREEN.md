# Quick Fix for Blank Screen Issue

## The Problem
The app is showing a blank screen (even Home page won't load). This is likely due to:
1. Browser caching old JavaScript files
2. Script loading order issues (now fixed)
3. Console errors preventing app initialization

## Changes Made
1. ✅ Moved Chart.js scripts to bottom of `<body>` in index.html (better loading order)
2. ✅ Removed `eval()` function check that could cause CSP issues
3. ✅ Simplified RenderCharts method

## Immediate Fix Steps

### Step 1: Clear Browser Cache
**Option A - Hard Refresh:**
- Windows/Linux: `Ctrl + Shift + R` or `Ctrl + F5`
- Mac: `Cmd + Shift + R`

**Option B - Clear Cache Manually:**
1. Press `F12` to open DevTools
2. Right-click the refresh button
3. Select "Empty Cache and Hard Reload"

**Option C - Incognito/Private Mode:**
- Open the app in a new incognito/private window

### Step 2: Build and Run Fresh
From PowerShell (not WSL):
```powershell
cd C:\Users\SujithQuintelier\source\repos\GitHub\sujithq\myenergy2\src\myenergy

# Clean build artifacts
Remove-Item -Recurse -Force bin,obj

# Rebuild
dotnet build

# Run
dotnet run
```

### Step 3: Check Browser Console
1. Open the app in your browser
2. Press `F12` to open Developer Tools
3. Go to **Console** tab
4. Look for errors (especially red text)

You should see:
```
charts.js loaded successfully
Chart.js available: true
```

If you see errors, share them and I can provide the specific fix.

## Common Console Errors and Fixes

### Error: "Failed to load resource: net::ERR_ABORTED"
**Cause:** Old cached files
**Fix:** Hard refresh (`Ctrl + Shift + R`)

### Error: "Uncaught SyntaxError" in charts.js
**Cause:** Corrupted file or syntax error
**Fix:** Re-copy charts.js from git

### Error: "Chart is not defined"
**Cause:** Chart.js CDN not loading
**Fix 1:** Check internet connection
**Fix 2:** Download Chart.js locally:
```powershell
# Download to wwwroot/lib/chartjs/
curl -o wwwroot/lib/chartjs/chart.min.js https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js
```

Then update index.html:
```html
<script src="lib/chartjs/chart.min.js"></script>
```

### Error: "Cannot read property 'DailyResults' of null"
**Cause:** Data loading issue
**Fix:** Check that data files exist in `wwwroot/Data/`

### White Screen / Nothing Loads
**Causes:**
1. Blazor not loading
2. JavaScript syntax error
3. CSP (Content Security Policy) blocking scripts

**Debug Steps:**
1. Check console for ANY errors
2. Check Network tab (F12 → Network) - are files loading?
3. Look for "Failed to fetch" or 404 errors

## Testing Without Battery Simulation
To test if the issue is specific to the Battery Simulation page:

1. Navigate to Home (`/`)
2. If Home loads, the core app is fine
3. Then try Battery Simulation (`/battery-simulation`)
4. If only Battery Simulation fails, it's a chart rendering issue
5. If Home also fails, it's a global app issue

## Still Not Working?

If after clearing cache and rebuilding you still see a blank screen:

1. **Check the browser console** (F12 → Console tab)
2. **Share the console output** - it will show the exact error
3. **Check Network tab** (F12 → Network) - see if files are 404ing

The console output will tell us exactly what's wrong!

## Reverting Changes (If Needed)

If you want to revert to the last working version:

```powershell
git status
git diff
git checkout -- src/myenergy/wwwroot/index.html
git checkout -- src/myenergy/Pages/BatterySimulation.razor
git checkout -- src/myenergy/wwwroot/js/charts.js
```

## Expected Behavior After Fix

1. **Home page loads** with solar dashboard
2. **Navigation works** between pages
3. **Battery Simulation page loads** showing:
   - Configuration controls
   - Metrics cards with costs
   - Empty chart areas (these need debugging separately)
4. **Browser console shows**:
   - "charts.js loaded successfully"
   - "Chart.js available: true"
   - Simulation logging messages

The charts being empty is a separate issue from the blank screen - we'll fix that once the app loads!
