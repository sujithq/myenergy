using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using myenergy;
using myenergy.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<EnergyDataService>();

// ODS Pricing Service - Choose implementation:
// - OdsPricingService (JSON): Simple, compatible, larger files (~18 MB)
// - OdsPricingParquetService (Parquet): 10x smaller files (~1.8 MB), faster loading
builder.Services.AddScoped<IOdsPricingService, OdsPricingParquetService>(); // ← Using JSON for now (Parquet needs fixes)
builder.Services.AddScoped<BatterySimulationService>();
builder.Services.AddScoped<RoiAnalysisService>();
builder.Services.AddScoped<PricingConfigService>();
builder.Services.AddScoped<DeviceProfileService>();
builder.Services.AddScoped<DataInitializationService>();
builder.Services.AddSingleton<AppConfigurationService>();

var host = builder.Build();

// Initialize all data at startup
var initService = host.Services.GetRequiredService<DataInitializationService>();
await initService.InitializeAsync();

await host.RunAsync();
