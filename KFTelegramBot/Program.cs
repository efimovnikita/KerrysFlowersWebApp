using KFTelegramBot.Providers;
using KFTelegramBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedLibrary.Providers;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(botToken))
        {
            throw new ArgumentException("Environment variable 'BOT_TOKEN' is empty");
        }

        var allowedUsersEnvVar = Environment.GetEnvironmentVariable("ALLOWED_USERS");
        if (string.IsNullOrWhiteSpace(allowedUsersEnvVar))
        {
            throw new ArgumentException("Environment variable 'ALLOWED_USERS' is empty");
        }

        var arrayOfIds = allowedUsersEnvVar.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s =>
        {
            var tryParse = long.TryParse(s, out var parsedResult);
            return tryParse ? parsedResult : 0;
        }).Where(l => l.Equals(0) == false).ToArray();

        if (arrayOfIds.Length == 0)
        {
            throw new ArgumentException("Allowed users count is 0");
        }

        var dbAdmin = Environment.GetEnvironmentVariable("DBADMIN");
        if (string.IsNullOrEmpty(dbAdmin))
        {
            throw new ArgumentNullException(nameof(dbAdmin), "Environment variable DBADMIN is not set.");
        }

        var dbPassword = Environment.GetEnvironmentVariable("DBPASSWORD");
        if (string.IsNullOrEmpty(dbPassword))
        {
            throw new ArgumentNullException(nameof(dbPassword), "Environment variable DBPASSWORD is not set.");
        }
        
        var dbHost = Environment.GetEnvironmentVariable("DBHOST");
        if (string.IsNullOrEmpty(dbHost))
        {
            throw new ArgumentNullException(nameof(dbHost), "Environment variable DBHOST is not set.");
        }
        
        var dbPort = Environment.GetEnvironmentVariable("DBPORT");
        if (string.IsNullOrEmpty(dbPort))
        {
            throw new ArgumentNullException(nameof(dbPort), "Environment variable DBPORT is not set.");
        }
        
        var dbDatabase = Environment.GetEnvironmentVariable("DBDATABASE");
        if (string.IsNullOrEmpty(dbDatabase))
        {
            throw new ArgumentNullException(nameof(dbDatabase), "Environment variable DBDATABASE is not set.");
        }

        string? openaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if(String.IsNullOrEmpty(openaiApiKey))
        {
            throw new ArgumentNullException(nameof(openaiApiKey), "Environment variable OPENAI_API_KEY is not set.");
        }

        services.AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, _) =>
            {
                TelegramBotClientOptions options = new(botToken);
                return new TelegramBotClient(options, httpClient);
            });

        services.AddSingleton<IMemoryStateProvider>(_ => new MemoryStateProvider(arrayOfIds));
        services.AddHttpClient();
        services.AddSingleton<IRecommendationsProvider>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var logger = provider.GetRequiredService<ILogger<RecommendationsProvider>>();
            return new RecommendationsProvider(openaiApiKey, httpClient, logger);
        });
        services.AddSingleton<IVioletRepository>(_ => new VioletRepository($"mongodb://{dbAdmin}:{dbPassword}@{dbHost}:{dbPort}", dbDatabase));

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
    })
    .Build();

await host.RunAsync();