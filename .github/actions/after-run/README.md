# After Run Actions - Composite Action

## Overview

This GitHub Actions composite action handles post-processing tasks after data refresh workflows complete. It copies generated data files, downloads the latest ODS pricing data from Elia in Parquet format, and commits all changes to the repository.

## What It Does

### 1. **Copy Data Files** 📋
Copies generated JSON data files from build output to web root and data directories:
- `data.json` → web root for Blazor app
- `consolidated.json` → web root for Blazor app  
- `data.json` → June.Data directory for backup

### 2. **Download ODS Parquet File** 📥
Downloads the latest ODS pricing data from Elia's Open Data Portal:
- **Source**: Elia Open Data API (ODS134 dataset)
- **Format**: Apache Parquet (binary columnar format)
- **Size**: ~1.8 MB (90% smaller than JSON equivalent ~18 MB)
- **Destination**: `./src/myenergy/wwwroot/Data/ods134.parquet`
- **Benefits**:
  - 10x faster loading in Blazor WebAssembly
  - Reduced bandwidth usage
  - Efficient columnar storage
  - Pre-downloaded data for offline-first experience

### 3. **Generate GitHub App Token** 🔑
Creates an authenticated token for committing changes back to the repository.

### 4. **Commit Changes** 💾
Commits and pushes any changed files:
- `data.json` - Main energy data
- `consolidated.json` - Aggregated statistics
- `ods134.parquet` - ODS pricing data from Elia

Each file is committed separately only if it has changes.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| `COMMAND` | Yes | Command name for commit messages (e.g., "June", "Sungrow") |
| `APP_ID` | Yes | GitHub App ID for authentication |
| `PRIVATE_KEY` | Yes | GitHub App private key for authentication |

## Usage

```yaml
- name: After Run Steps
  uses: ./.github/actions/after-run
  with:
    COMMAND: 'June'
    APP_ID: ${{ secrets.APP_ID }}
    PRIVATE_KEY: ${{ secrets.PRIVATE_KEY }}
```

## Example Workflow Integration

```yaml
name: Update Energy Data

on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM
  workflow_dispatch:

jobs:
  update-data:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
          
      - name: Run data refresh
        run: |
          cd src/June.Data
          dotnet run -- june
          
      - name: After Run Steps
        uses: ./.github/actions/after-run
        with:
          COMMAND: 'June'
          APP_ID: ${{ secrets.APP_ID }}
          PRIVATE_KEY: ${{ secrets.PRIVATE_KEY }}
```

## File Operations

### Copy Operations
```bash
# Source → Destination
./src/June.Data/bin/Release/net9.0/Data/data.json 
  → ./src/myenergy/wwwroot/Data/data.json

./src/June.Data/bin/Release/net9.0/Data/consolidated.json 
  → ./src/myenergy/wwwroot/Data/consolidated.json

./src/June.Data/bin/Release/net9.0/Data/data.json 
  → ./src/June.Data/Data/data.json
```

### Download Operation
```bash
# Elia ODS Parquet Download
URL: https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/parquet?lang=en&timezone=Europe%2FBrussels
Destination: ./src/myenergy/wwwroot/Data/ods134.parquet
Expected Size: ~1.8 MB
```

## Commit Messages

The action generates descriptive commit messages for each file type:

```
📊 Update data file for June
📊 Update consolidated file for Sungrow  
📊 Update ODS Parquet file for MeteoStat
```

## Error Handling

- **Missing source files**: Logs warning but continues (non-fatal)
- **Download failure**: Exits with error code 1 (fatal)
- **No changes**: Skips commit for unchanged files

## Benefits of Parquet Download

### Performance Comparison

| Format | Size | Load Time | Bandwidth |
|--------|------|-----------|-----------|
| JSON | ~18 MB | ~3-5s | High |
| **Parquet** | **~1.8 MB** | **~0.3-0.5s** | **Low** |

### Why Download in CI/CD?

1. **Pre-cache data**: Users get instant load times
2. **Reduced API calls**: No client-side downloads to Elia
3. **Offline-first**: App works without external API access
4. **Version control**: Track ODS pricing data changes over time
5. **Reliability**: No dependency on Elia API availability at runtime

## Technical Details

### Parquet Format
- **Type**: Apache Parquet (columnar storage)
- **Compression**: Built-in compression (Snappy/GZIP)
- **Schema**: Dynamic column detection
- **Fields**: DateTime, MarginalIncrementalPrice, MarginalDecrementalPrice

### Used By
- `OdsPricingParquetService.cs` - Blazor WebAssembly service
- `IOdsPricingService` interface implementation
- All pricing-related pages (SmartUsageAdvisor, BatterySimulation, etc.)

## Troubleshooting

### Download Fails
```bash
# Check URL accessibility
curl -I "https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/parquet?lang=en&timezone=Europe%2FBrussels"

# Verify file size
ls -lh ./src/myenergy/wwwroot/Data/ods134.parquet
```

### Commit Fails
```bash
# Check git status
git status

# Verify token permissions
# Ensure GitHub App has Contents: Read and write permissions
```

### File Not Found
```bash
# Ensure data files were built
ls -la ./src/June.Data/bin/Release/net9.0/Data/

# Check directory structure
tree ./src/myenergy/wwwroot/Data/
```

## Maintenance

### Updating Parquet URL
If Elia changes their API endpoint:

1. Update `PARQUET_URL` in the action
2. Update `ELIA_API_URL` in `OdsPricingParquetService.cs`
3. Test download manually
4. Update this documentation

### Adding New Data Files

To add additional files to copy/commit:

```yaml
# In copy step:
copy "./src/NewSource/data.json" "./src/myenergy/wwwroot/Data/newfile.json"

# In commit step:
commit_if_changed "./src/myenergy/wwwroot/Data/newfile.json" "new data file"
```

## Version History

- **v2.0** (Dec 2024): Added ODS Parquet download, refactored commit logic
- **v1.0** (Earlier): Initial version with JSON file copying

## Related Documentation

- [ODS_SERVICE_INTERFACE_REFACTORING.md](../../ODS_SERVICE_INTERFACE_REFACTORING.md) - Service architecture
- [OdsPricingParquetService.cs](../../src/myenergy/Services/OdsPricingParquetService.cs) - Parquet service implementation
- [Elia Open Data Portal](https://opendata.elia.be/) - Data source

---

**Status**: ✅ Production Ready  
**Last Updated**: December 2024  
**Maintained By**: Sujith Quintelier
