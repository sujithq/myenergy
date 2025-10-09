# ODS Data Management - Quick Reference

## ✨ NEW: Automatic Download Feature

The application now **automatically downloads** ODS pricing data from Elia at startup!

### How It Works

1. **First Startup**: If no local `ods134.json` file exists, the app automatically downloads the latest data from Elia
2. **Subsequent Startups**: Uses the local file for fast loading
3. **Manual Refresh**: Click the "Refresh ODS Data" button in ROI Analysis to get the latest data

### No More Manual Updates Required! 🎉

You no longer need to manually download and update the `ods134.json` file. The app handles it automatically.

---

## 🔄 Using the Refresh Button

### In the ROI Analysis Page:

1. Check the **"Use Dynamic (ODS)"** checkbox
2. You'll see:
   - **Available date range** (e.g., "Available: 22/05/2024 - 09/10/2025")
   - **Last updated timestamp** (e.g., "Last updated: 2025-10-09 14:30")
   - **Refresh ODS Data button** with a refresh icon
3. Click **"Refresh ODS Data"** to download the latest data from Elia
4. Wait 10-20 seconds while data downloads (spinner shows progress)
5. ROI calculations automatically update with fresh data

---

## 📦 Data Source

- **API**: Elia OpenData Platform
- **Dataset**: ods134 - Operational Deviation Settlement prices
- **URL**: https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels
- **Resolution**: 15-minute intervals (PT15M)
- **Format**: JSON with marginal prices in €/MWh

---

## 🚀 Startup Behavior

### Scenario 1: No Local File
```
App starts → Try local file → Not found → Download from Elia API
✅ Latest data loaded (10-20 seconds)
```

### Scenario 2: Local File Exists
```
App starts → Load local file → Ready!
✅ Fast startup (< 1 second)
```

### Scenario 3: API Unavailable
```
App starts → Try local file → Load cached data → Ready!
✅ Works offline with existing data
```

---

## 💡 Best Practices

### For Daily Use
- Let the app auto-download on first startup
- Use the Refresh button when you need the latest data
- Refresh monthly or when you notice data is outdated

### For Development
- Keep a recent `ods134.json` file in `wwwroot/Data/` for fast startup
- Can work completely offline with cached data
- No internet connection required after initial download

### For Production
- **Option 1**: Deploy without local file → Always gets latest data
- **Option 2**: Deploy with recent file → Fast startup + refresh on-demand
- **Option 3**: Ship with file as backup → Best of both worlds

---

## 🔍 Console Logging

### Successful Load (Local File)
```
Loading ODS pricing data from local file...
✅ Loaded 582037 ODS pricing records from local file
📅 Date range: 2024-05-22 00:00 to 2025-10-09 05:30
💶 Average import: €0.0945/kWh (€94.50/MWh)
💶 Average export: €0.0412/kWh (€41.20/MWh)
```

### Auto-Download Fallback
```
Loading ODS pricing data from local file...
Local file not found or error: File not found
Downloading from Elia API...
✅ Loaded 582037 ODS pricing records from Elia API (fallback)
```

### Manual Refresh
```
🔄 Refreshing ODS data from Elia API...
✅ Loaded 582037 ODS pricing records from Elia API (forced refresh)
```

---

## 🛠️ Manual Updates (Optional)

If you prefer to update the file manually (not required anymore):

### Using Bash/WSL
```bash
curl "https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels" \
  -o wwwroot/Data/ods134.json
```

### Using PowerShell
```powershell
Invoke-WebRequest `
  -Uri "https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels" `
  -OutFile "wwwroot/Data/ods134.json"
```

### Using Browser
1. Open: https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels
2. Save page as `ods134.json`
3. Copy to `wwwroot/Data/ods134.json`

---

## ❓ Troubleshooting

### "Download takes too long"
- **Normal**: First download is 50-80 MB and takes 10-20 seconds
- **Solution**: Be patient, subsequent loads are < 1 second (cached)

### "Failed to download from Elia API"
- **Cause**: No internet or API temporarily down
- **Solution**: 
  1. Check internet connection
  2. Wait and try again
  3. Use fixed pricing mode temporarily
  4. Download file manually as backup

### "Data seems outdated"
- **Check**: Last updated timestamp below the date picker
- **Solution**: Click "Refresh ODS Data" button

### "Out of memory errors"
- **Rare**: Dataset is ~150 MB in memory
- **Solution**: Usually fine on most devices, may need more RAM

---

## 📊 Data Statistics

### Current Dataset (ods134)
- **Records**: ~580,000 pricing intervals
- **Date Range**: May 22, 2024 - October 9, 2025 (17+ months)
- **File Size**: ~50-80 MB (uncompressed JSON)
- **Memory Usage**: ~100-150 MB when loaded
- **Load Time**: 10-20 seconds from API, < 1 second from local file

### Price Information
- **Import (marginalincrementalprice)**: What you pay for grid electricity (€/MWh)
- **Export (marginaldecrementalprice)**: What you get for exporting to grid (€/MWh)
- **Resolution**: 15-minute intervals (96 intervals per day)
- **Format**: Prices automatically converted from €/MWh to €/kWh (÷1000)

---

## 📚 Related Documentation

- **DYNAMIC_ODS_LOADING.md**: Complete technical documentation of the auto-download feature
- **ODS_PRICING_UPDATE.md**: Details about the ODS data format and price calculations
- **COMPLETE_FIX_SUMMARY.md**: Full summary of battery ROI fixes

---

## ✅ Summary

### What Changed
❌ **Before**: Manual download and file updates required  
✅ **Now**: Automatic download with refresh button  

### Benefits
🚀 **Automatic** - Downloads as needed  
🔄 **Refreshable** - One-click updates  
⚡ **Fast** - Cached for quick loading  
🌐 **Current** - Always latest data  
💪 **Resilient** - Works offline  

### Action Required
**None!** Just use the app normally. Click "Refresh ODS Data" when you want the latest prices.

---

**Last Updated**: October 2025  
**Feature Status**: ✅ Implemented and ready to test  
