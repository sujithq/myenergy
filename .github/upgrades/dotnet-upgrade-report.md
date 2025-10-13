# .NET 10.0 Upgrade Report

## Project target framework modifications

| Project name                | Old Target Framework | New Target Framework | Commits                                    |
|:----------------------------|:--------------------:|:--------------------:|:-------------------------------------------|
| myenergy\\myenergy.csproj   | net9.0               | net10.0              | 4bea10df, adb82d80                         |
| June.Data\\June.Data.csproj | net9.0               | net10.0              | 19eecfc1, 24d49c16                         |
| Common\\Common.csproj        | net9.0               | net10.0              | 14b28ccd                                   |

## NuGet Packages

| Package Name                                            | Old Version | New Version               | Commit ID                                  |
|:--------------------------------------------------------|:-----------:|:-------------------------:|:-------------------------------------------|
| Microsoft.AspNetCore.Components.WebAssembly             | 9.0.9       | 10.0.0-rc.1.25451.107     | adb82d80                                   |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer   | 9.0.9       | 10.0.0-rc.1.25451.107     | adb82d80                                   |
| Microsoft.Extensions.Configuration                      | 9.0.9       | 10.0.0-rc.1.25451.107     | 24d49c16                                   |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 9.0.7       | 10.0.0-rc.1.25451.107     | 24d49c16                                   |
| Microsoft.Extensions.Configuration.UserSecrets          | 9.0.9       | 10.0.0-rc.1.25451.107     | 24d49c16                                   |
| Microsoft.Extensions.DependencyInjection                | 9.0.7       | 10.0.0-rc.1.25451.107     | 24d49c16                                   |
| Microsoft.Extensions.DependencyInjection.Abstractions   | 9.0.7       | 10.0.0-rc.1.25451.107     | 24d49c16                                   |
| Microsoft.Extensions.Options                            | 9.0.7       | 10.0.0-rc.1.25451.107     | 24d49c16                                   |
| Microsoft.Extensions.Options.ConfigurationExtensions    | 9.0.7       | 10.0.0-rc.1.25451.107     | 24d49c16                                   |
| System.Configuration.ConfigurationManager               | 9.0.7       | 10.0.0-rc.1.25451.107     | 24d49c16                                   |

## All commits

| Commit ID | Description                                                                                           |
|:----------|:------------------------------------------------------------------------------------------------------|
| ba5ff6fe  | Commit upgrade plan                                                                                   |
| 4bea10df  | Update target framework to net10.0 in myenergy.csproj                                                 |
| adb82d80  | Update myenergy.csproj to Blazor WebAssembly 10.0.0-rc                                                |
| 19eecfc1  | Bump target framework to net10.0 in June.Data.csproj                                                  |
| 24d49c16  | Update NuGet package versions in June.Data.csproj                                                     |
| 14b28ccd  | Update target framework to net10.0 in Common.csproj                                                   |
| 204004ff  | Updated all 6 workflow files to use .NET 10.0.x                                                       |

## GitHub Actions Workflow Updates

All GitHub Actions workflow files have been updated to use .NET 10.0.x:

- `.github/workflows/Charge.yml` - Updated GLOBAL_DOTNET_VERSION from 9.0.x to 10.0.x
- `.github/workflows/JuneData.yml` - Updated GLOBAL_DOTNET_VERSION from 9.0.x to 10.0.x
- `.github/workflows/main.yml` - Updated dotnet-version from 9.0.x to 10.0.x
- `.github/workflows/MeteoStatData.yml` - Updated GLOBAL_DOTNET_VERSION from 9.0.x to 10.0.x
- `.github/workflows/SungrowData.yml` - Updated GLOBAL_DOTNET_VERSION from 9.0.x to 10.0.x
- `.github/workflows/SunRiseSet.yml` - Updated GLOBAL_DOTNET_VERSION from 9.0.x to 10.0.x

## Next steps

- Test your Blazor WebAssembly application locally to ensure it runs correctly with .NET 10.0
- Review any deprecation warnings or breaking changes in .NET 10.0 that may affect your application
- Consider testing the GitHub Actions workflows in your CI/CD pipeline
- Monitor for any runtime issues when deploying to production
- Keep track of .NET 10.0 preview updates and consider upgrading to the final release when available
