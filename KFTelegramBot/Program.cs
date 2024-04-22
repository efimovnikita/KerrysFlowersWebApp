using KFTelegramBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
        if (String.IsNullOrWhiteSpace(botToken))
        {
            throw new ArgumentException("Environment variable 'BOT_TOKEN' is empty");
        }

        var allowedUsersEnvVar = Environment.GetEnvironmentVariable("ALLOWED_USERS");
        if (String.IsNullOrWhiteSpace(allowedUsersEnvVar))
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

        services.AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                TelegramBotClientOptions options = new(botToken);
                return new TelegramBotClient(options, httpClient);
            });

        services.AddSingleton<IMemoryStateProvider>(_ => new MemoryStateProvider(arrayOfIds));

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
    })
    .Build();

await host.RunAsync();