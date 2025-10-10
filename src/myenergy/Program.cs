using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using myenergy;
using myenergy.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<EnergyDataService>();
builder.Services.AddScoped<OdsPricingService>();
builder.Services.AddScoped<BatterySimulationService>();
builder.Services.AddScoped<RoiAnalysisService>();
builder.Services.AddScoped<PricingConfigService>();
builder.Services.AddScoped<DataInitializationService>();
builder.Services.AddSingleton<AppConfigurationService>();

var host = builder.Build();

// Initialize all data at startup
var initService = host.Services.GetRequiredService<DataInitializationService>();
await initService.InitializeAsync();

await host.RunAsync();
