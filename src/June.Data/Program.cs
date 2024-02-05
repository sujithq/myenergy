using June.Data.Commands;
using June.Data.Commands.Settings;
using MeteoStat.Data.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spectre.Console.Cli;
using Sungrow.Data.Commands;

namespace June.Data
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                                devEnvironmentVariable.Equals("development", StringComparison.CurrentCultureIgnoreCase);

            //Determines the working environment as IHostingEnvironment is unavailable in a console app

            var builder = new ConfigurationBuilder();
            // tell the builder to look for the appsettings.json file
            builder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // only add secrets in development
            if (isDevelopment)
            {
                builder
                    .AddUserSecrets<Program>();
            }

            var Configuration = builder.Build();

            var serviceProvider = ConfigureServices(Configuration);


            var app = new CommandApp(new TypeRegistrar(serviceProvider));

            app.Configure(config =>
            {
                config.AddBranch<BaseCommandSettings>("June", add =>
                {
                    add.AddCommand<JuneRunCommand>("run");
                });
                config.AddBranch<BaseCommandSettings>("Sungrow", add =>
                {
                    add.AddCommand<SungrowRunCommand>("run");
                });
                config.AddBranch<BaseCommandSettings>("MeteoStat", add =>
                {
                    add.AddCommand<MeteoStatRunCommand>("run");
                });
                config.AddBranch<BaseCommandSettings>("Charge", add =>
                {
                    add.AddCommand<ChargeRunCommand>("run");
                });
                config.AddBranch<BaseCommandSettings>("SunRiseSet", add =>
                {
                    add.AddCommand<SunRiseSetRunCommand>("run");
                });
            });

            return app.Run(args);
        }

        static IServiceProvider ConfigureServices(IConfigurationRoot configuration)
        {
            IServiceCollection services = new ServiceCollection();

            //Map the implementations of your classes here ready for DI
            _ = services
                .Configure<JuneSettings>(configuration.GetSection(nameof(JuneSettings)))
                .Configure<SungrowSettings>(configuration.GetSection(nameof(SungrowSettings)))
                .Configure<MeteoStatSettings>(configuration.GetSection(nameof(MeteoStatSettings)))
                .Configure<ChargeSettings>(configuration.GetSection(nameof(ChargeSettings)))
                .Configure<SunRiseSetSettings>(configuration.GetSection(nameof(SunRiseSetSettings)))
                .AddSingleton<JuneRunSettings>()
                .AddSingleton<SungrowRunSettings>()
                .AddSingleton<MeteoStatRunSettings>()
                .AddSingleton<ChargeRunSettings>()
                .AddSingleton<SunRiseSetRunSettings>()
                .AddSingleton(sp => { 
                    return new JuneRunCommand(sp.GetRequiredService<IOptions<JuneSettings>>(), new JuneScraper(sp.GetRequiredService<IOptions<JuneSettings>>()));
                })
                .AddSingleton(sp => {
                    return new SungrowRunCommand(sp.GetRequiredService<IOptions<SungrowSettings>>(), new SungrowScraper(sp.GetRequiredService<IOptions<SungrowSettings>>()));
                })
                .AddSingleton(sp => {
                    return new MeteoStatRunCommand(sp.GetRequiredService<IOptions<MeteoStatSettings>>(), new MeteoStatScraper(sp.GetRequiredService<IOptions<MeteoStatSettings>>()));
                })
                .AddSingleton(sp => {
                    return new ChargeRunCommand(sp.GetRequiredService<IOptions<ChargeSettings>>());
                }) 
                .AddSingleton(sp => {
                    return new SunRiseSetRunCommand(sp.GetRequiredService<IOptions<SunRiseSetSettings>>());//, new SunRiseSetScraper(sp.GetRequiredService<IOptions<SunRiseSetSettings>>()));
                })
                .AddOptions()
                .BuildServiceProvider();

            return services.BuildServiceProvider();
        }

    }
}
