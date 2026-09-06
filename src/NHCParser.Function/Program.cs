using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHCParser.Core.Services;
using NHCParser.Function.Services;
using TTENET.TTEBusiness.Core.Models;
using TTENET.TTEBusiness.Core.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, configurationBuilder) =>
    {
        configurationBuilder
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();

        services.Configure<NHCParserOptions>(context.Configuration.GetSection(NHCParserOptions.SectionName));
        services.AddSingleton<INhcAdvisoryParser, NhcAdvisoryParser>();
        services.AddSingleton<INhcTropicalWeatherDiscussionParser, NhcTropicalWeatherDiscussionParser>();
        services.AddSingleton<NhcAdvisoryClassifier>();
        services.AddSingleton<INhcAdvisoryProcessor, PersistingParsedAdvisoryProcessor>();
        services.AddSingleton<INhcParserRunner, NhcParserRunner>();
        services.AddSingleton<ITteRepository, TteRepository>();
        services.AddHostedService<NhcParserStartupService>();
        services.AddHttpClient<INhcAdvisoryClient, NhcAdvisoryClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<NHCParserOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.SourceTimeoutSeconds));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NHCParser.Function/1.0");
        });
    })
    .Build();

host.Run();
