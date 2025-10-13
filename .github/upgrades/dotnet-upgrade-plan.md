# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade myenergy\myenergy.csproj
4. Upgrade June.Data\June.Data.csproj
5. Upgrade Common\Common.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

No projects are excluded from the upgrade.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                              | Current Version | New Version               | Description                         |
|:----------------------------------------------------------|:---------------:|:-------------------------:|:------------------------------------|
| Microsoft.AspNetCore.Components.WebAssembly               | 9.0.9           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer     | 9.0.9           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| Microsoft.Extensions.Configuration                        | 9.0.9           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| Microsoft.Extensions.Configuration.EnvironmentVariables   | 9.0.7           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| Microsoft.Extensions.Configuration.UserSecrets            | 9.0.9           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| Microsoft.Extensions.DependencyInjection                  | 9.0.7           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| Microsoft.Extensions.DependencyInjection.Abstractions     | 9.0.7           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| Microsoft.Extensions.Options                              | 9.0.7           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| Microsoft.Extensions.Options.ConfigurationExtensions      | 9.0.7           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |
| System.Configuration.ConfigurationManager                 | 9.0.7           | 10.0.0-rc.1.25451.107     | Recommended for .NET 10.0           |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### myenergy\myenergy.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Components.WebAssembly should be updated from `9.0.9` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Components.WebAssembly.DevServer should be updated from `9.0.9` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)

#### June.Data\June.Data.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.Extensions.Configuration should be updated from `9.0.9` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Configuration.EnvironmentVariables should be updated from `9.0.7` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Configuration.UserSecrets should be updated from `9.0.9` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.DependencyInjection should be updated from `9.0.7` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.DependencyInjection.Abstractions should be updated from `9.0.7` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Options should be updated from `9.0.7` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Options.ConfigurationExtensions should be updated from `9.0.7` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)
  - System.Configuration.ConfigurationManager should be updated from `9.0.7` to `10.0.0-rc.1.25451.107` (*recommended for .NET 10.0*)

#### Common\Common.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`
