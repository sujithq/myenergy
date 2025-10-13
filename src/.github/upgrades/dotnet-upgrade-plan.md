# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade myenergy\myenergy.csproj
4. Upgrade June.Data\June.Data.csproj
5. Upgrade Common\Common.csproj
6. Update GitHub Actions workflows to use .NET 10.0

## Settings

// ...existing code...

### GitHub Actions workflow modifications

All workflow files in `.github/workflows/` need to update the `GLOBAL_DOTNET_VERSION` environment variable from `9.0.x` to `10.0.x`:

- Charge.yml
- JuneData.yml
- main.yml
- MeteoStatData.yml
- SungrowData.yml
- SunRiseSet.yml
