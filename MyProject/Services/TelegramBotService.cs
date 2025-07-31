using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramSignalBot.Models;
using TelegramSignalBot.Services;

namespace TelegramSignalBot.Services
{
    public class TelegramBotService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly SignalParser _signalParser;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISignalProcessor _signalProcessor;
        private CancellationTokenSource _cancellationTokenSource = new();

        public TelegramBotService(
            IConfiguration configuration,
            SignalParser signalParser,
            ISignalProcessor signalProcessor,
            ILogger<TelegramBotService> logger)
        {
            _configuration = configuration;
            _signalParser = signalParser;
            _signalProcessor = signalProcessor;
            _logger = logger;

            var token = _configuration["TelegramBot:Token"];
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Telegram bot token is not configured");

            _botClient = new TelegramBotClient(token);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Telegram Bot Service...");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message },
                ThrowPendingUpdates = true,
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cancellationTokenSource.Token
            );

            _logger.LogInformation("Telegram Bot Service started successfully");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Telegram Bot Service...");
            _cancellationTokenSource.Cancel();
            _logger.LogInformation("Telegram Bot Service stopped");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            var username = message.From?.Username ?? "Unknown";

            _logger.LogInformation($"Received message from {username} in chat {chatId}: {messageText}");

            // Check if this chat is allowed
            var allowedChatIds = _configuration.GetSection("TelegramBot:AllowedChatIds").Get<long[]>();
            if (allowedChatIds != null && allowedChatIds.Length > 0 && !allowedChatIds.Contains(chatId))
            {
                _logger.LogWarning($"Unauthorized access attempt from chat {chatId}");
                return;
            }

            try
            {
                // Parse the message for trading signals
                var signal = _signalParser.ParseSignal(messageText);

                if (signal != null)
                {
                    _logger.LogInformation($"Parsed signal: {signal}");
                    
                    // Process the signal
                    await _signalProcessor.ProcessSignalAsync(signal);
                    
                    // Send confirmation to user
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"✅ Signal processed: {signal.Type} {signal.Symbol}",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    _logger.LogDebug($"Message did not contain a valid trading signal: {messageText}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message: {messageText}");
                
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Error processing signal. Please check the format and try again.",
                    cancellationToken: cancellationToken
                );
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(ErrorMessage);
            return Task.CompletedTask;
        }
    }
} 