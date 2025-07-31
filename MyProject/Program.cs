using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramSignalBot.Services;

namespace TelegramSignalBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Configure services
                    ConfigureServices(services, context.Configuration);
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

        static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Signal parser
            var signalKeywords = new Dictionary<SignalType, string[]>
            {
                [SignalType.Buy] = configuration.GetSection("SignalSettings:SignalKeywords:Buy").Get<string[]>() ?? new[] { "BUY", "LONG", "BUY_SIGNAL" },
                [SignalType.Sell] = configuration.GetSection("SignalSettings:SignalKeywords:Sell").Get<string[]>() ?? new[] { "SELL", "SHORT", "SELL_SIGNAL" },
                [SignalType.Close] = configuration.GetSection("SignalSettings:SignalKeywords:Close").Get<string[]>() ?? new[] { "CLOSE", "EXIT", "CLOSE_POSITION" }
            };
            services.AddSingleton<SignalParser>(new SignalParser(signalKeywords));

            // Signal processor
            services.AddSingleton<ISignalProcessor, SignalProcessor>();

            // Telegram bot service
            services.AddHostedService<TelegramBotService>();
        }
    }
} 