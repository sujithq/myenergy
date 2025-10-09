# How to Update ODS Pricing Data

## Dataset Information

**Dataset Name:** `ods134`  
**Description:** Operational Deviation Settlement (ODS) prices for the Belgian electricity grid  
**Provider:** Elia Group (Belgian TSO)  
**Resolution:** 15-minute intervals (PT15M)  
**Timezone:** Europe/Brussels (CET/CEST)

## Current Data

- **File:** `wwwroot/Data/ods134.json`
- **Records:** ~580,000+ pricing intervals
- **Date Range:** May 22, 2024 - October 9, 2025 (17+ months)
- **Size:** Large file (~580k lines of JSON)

## Data Fields

Each record contains:
```json
{
    "datetime": "2025-10-09T05:30:00+02:00",
    "resolutioncode": "PT15M",
    "qualitystatus": "NotValidated",
    "ace": 15.771,
    "systemimbalance": 130.277,
    "alpha": 0.0,
    "alpha_prime": 0.0,
    "marginalincrementalprice": 118.8,     // Import price (€/MWh)
    "marginaldecrementalprice": 87.773,    // Export price (€/MWh)
    "imbalanceprice": 87.773
}
```

### Key Fields Used by Application:
- **`datetime`**: Timestamp of the 15-minute interval
- **`marginalincrementalprice`**: Price to import from grid (€/MWh)
- **`marginaldecrementalprice`**: Price to export to grid (€/MWh)

## How to Update the Data

### Method 1: Direct API Download (Recommended)

**API Endpoint:**
```
https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels
```

**Steps:**
1. Open the URL in your browser or use curl/wget
2. Save the response as `wwwroot/Data/ods134.json`
3. Restart the application to load the new data

**Using curl (Linux/Mac/WSL):**
```bash
cd wwwroot/Data
curl -o ods134.json "https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels"
```

**Using PowerShell (Windows):**
```powershell
cd wwwroot\Data
Invoke-WebRequest -Uri "https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels" -OutFile ods134.json
```

### Method 2: Elia Open Data Portal

1. Visit: https://opendata.elia.be/
2. Search for dataset: **"ods134"** or **"Operational Deviation Settlement"**
3. Select export format: **JSON**
4. Set timezone: **Europe/Brussels**
5. Download the file
6. Replace `wwwroot/Data/ods134.json` with the downloaded file

### Method 3: API with Date Filtering (For Specific Periods)

To get only specific date ranges, use the API with filters:

**Example: Last 30 days**
```
https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?where=datetime%20%3E%3D%20date%272024-09-09%27&timezone=Europe%2FBrussels
```

**Example: Specific date range**
```
https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?where=datetime%20%3E%3D%20date%272024-05-01%27%20AND%20datetime%20%3C%3D%20date%272024-12-31%27&timezone=Europe%2FBrussels
```

**Query Parameters:**
- `where`: Filter expression (optional)
- `timezone`: Set to `Europe/Brussels` for correct local times
- `lang`: Language (nl/en/fr/de)
- `limit`: Maximum records (default: all)

## Data Update Frequency

**Official Data Updates:**
- **Real-time data:** Updated every 15 minutes (with "NotValidated" status)
- **Validated data:** Usually within 24-48 hours
- **Historical data:** Available from May 2024 onwards

**Recommended Update Schedule:**
- **For current analysis:** Update weekly or monthly
- **For historical analysis:** No need to update unless extending date range
- **For real-time tracking:** Could fetch daily/hourly (but increases load time)

## Application Behavior

**Data Loading:**
- File is loaded once on first access
- Parsed into memory (List<OdsPricing>)
- Indexed by DateTime for fast lookups
- Logs are written to console with data range and statistics

**Console Output Example:**
```
Loaded 580128 ODS pricing records from ods134.json
Date range: 2024-05-22 00:00 to 2025-10-09 05:30
Average import price: €0.0953/kWh (€95.30/MWh)
Average export price: €0.0412/kWh (€41.20/MWh)
```

## File Size Considerations

**Current File (~580k lines):**
- JSON file size: ~50-80 MB
- Memory usage: ~100-150 MB when loaded
- Load time: ~2-5 seconds on modern hardware

**Performance Tips:**
- File is loaded only once per application session
- Dictionary index provides O(1) lookup by DateTime
- If file becomes too large (>100 MB), consider:
  - Splitting by year
  - Filtering to relevant date ranges only
  - Using a database instead of JSON

## Data Quality

**Quality Status Values:**
- **`NotValidated`**: Recent data, not yet verified
- **`DataIssue`**: Known issue with this interval
- **`Validated`**: Confirmed accurate data
- **`Estimated`**: Calculated/interpolated value

The application uses all data regardless of quality status. To exclude certain quality levels, you would need to filter in the OdsPricingService.

## Troubleshooting

**Problem: "Error loading ODS pricing data"**
- Check if file exists: `wwwroot/Data/ods134.json`
- Verify JSON format is valid
- Check file permissions

**Problem: "No pricing data for my dates"**
- Check the date range in console logs
- Verify your energy data dates match ODS data range
- Download latest data from Elia API

**Problem: "File is too large / slow to load"**
- Consider filtering API download to specific date range
- Only download dates matching your energy data
- Use compression (gzip) if server supports it

## API Documentation

Full API documentation: https://opendata.elia.be/api/v2/console

**Useful Endpoints:**
- **List all datasets:** `/api/explore/v2.1/catalog/datasets`
- **Dataset metadata:** `/api/explore/v2.1/catalog/datasets/ods134`
- **Export JSON:** `/api/explore/v2.1/catalog/datasets/ods134/exports/json`
- **Query records:** `/api/explore/v2.1/catalog/datasets/ods134/records`

## Automation Ideas

### PowerShell Script (Windows)
```powershell
# update-ods-data.ps1
$url = "https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels"
$output = "wwwroot\Data\ods134.json"

Write-Host "Downloading latest ODS data..."
Invoke-WebRequest -Uri $url -OutFile $output
Write-Host "Download complete: $output"
```

### Bash Script (Linux/Mac/WSL)
```bash
#!/bin/bash
# update-ods-data.sh
URL="https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels"
OUTPUT="wwwroot/Data/ods134.json"

echo "Downloading latest ODS data..."
curl -o "$OUTPUT" "$URL"
echo "Download complete: $OUTPUT"
```

### Scheduled Task
- **Windows:** Use Task Scheduler to run PowerShell script weekly
- **Linux:** Use cron job to run bash script weekly
- **Azure/Cloud:** Use Azure Functions or Logic Apps for scheduled updates

## Notes

- **Belgium-specific:** This data is specific to the Belgian electricity grid
- **DST-aware:** Timestamps include timezone offset (+01:00 or +02:00)
- **Gap handling:** If no exact match, application rounds to nearest 15-minute interval
- **Missing data:** Returns null if no pricing data available for requested time
- **Backward compatible:** Model supports both old (aFRR/mFRR) and new (marginal) formats

---

**Last Updated:** January 2025  
**Data Source:** Elia Group - Belgian Transmission System Operator  
**Dataset ID:** ods134
