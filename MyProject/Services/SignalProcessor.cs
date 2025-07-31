using TelegramSignalBot.Models;
using System.Text.Json;

namespace TelegramSignalBot.Services
{
    public class SignalProcessor : ISignalProcessor
    {
        private readonly ILogger<SignalProcessor> _logger;
        private readonly IConfiguration _configuration;
        private readonly Queue<TradingSignal> _signalQueue = new();
        private readonly object _queueLock = new();
        private readonly string _signalFilePath;

        public SignalProcessor(ILogger<SignalProcessor> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _signalFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "signals.json");
        }

        public async Task ProcessSignalAsync(TradingSignal signal)
        {
            try
            {
                _logger.LogInformation($"Processing signal: {signal}");

                // Validate signal
                if (!ValidateSignal(signal))
                {
                    _logger.LogWarning($"Invalid signal received: {signal}");
                    return;
                }

                // Add signal to queue
                lock (_queueLock)
                {
                    _signalQueue.Enqueue(signal);
                }

                // Save signal to file for cTrader bot to read
                await SaveSignalToFile(signal);

                _logger.LogInformation($"Signal queued successfully: {signal.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing signal: {signal}");
            }
        }

        private bool ValidateSignal(TradingSignal signal)
        {
            if (string.IsNullOrEmpty(signal.Symbol))
            {
                _logger.LogWarning("Signal missing symbol");
                return false;
            }

            if (signal.Type == SignalType.Unknown)
            {
                _logger.LogWarning("Signal type is unknown");
                return false;
            }

            // For buy/sell signals, we need at least a symbol
            if ((signal.Type == SignalType.Buy || signal.Type == SignalType.Sell) && 
                string.IsNullOrEmpty(signal.Symbol))
            {
                _logger.LogWarning("Buy/Sell signal missing symbol");
                return false;
            }

            return true;
        }

        private async Task SaveSignalToFile(TradingSignal signal)
        {
            try
            {
                var signals = new List<TradingSignal>();

                // Read existing signals if file exists
                if (File.Exists(_signalFilePath))
                {
                    var existingContent = await File.ReadAllTextAsync(_signalFilePath);
                    if (!string.IsNullOrEmpty(existingContent))
                    {
                        signals = JsonSerializer.Deserialize<List<TradingSignal>>(existingContent) ?? new List<TradingSignal>();
                    }
                }

                // Add new signal
                signals.Add(signal);

                // Keep only the last 100 signals to prevent file from growing too large
                if (signals.Count > 100)
                {
                    signals = signals.TakeLast(100).ToList();
                }

                // Write back to file
                var jsonContent = JsonSerializer.Serialize(signals, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_signalFilePath, jsonContent);

                _logger.LogDebug($"Signal saved to file: {signal.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving signal to file");
            }
        }

        public List<TradingSignal> GetUnprocessedSignals()
        {
            lock (_queueLock)
            {
                var signals = new List<TradingSignal>();
                while (_signalQueue.Count > 0)
                {
                    signals.Add(_signalQueue.Dequeue());
                }
                return signals;
            }
        }

        public void MarkSignalAsProcessed(string signalId)
        {
            try
            {
                if (File.Exists(_signalFilePath))
                {
                    var content = File.ReadAllText(_signalFilePath);
                    var signals = JsonSerializer.Deserialize<List<TradingSignal>>(content) ?? new List<TradingSignal>();
                    
                    var signal = signals.FirstOrDefault(s => s.Id == signalId);
                    if (signal != null)
                    {
                        signal.IsProcessed = true;
                        
                        var jsonContent = JsonSerializer.Serialize(signals, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_signalFilePath, jsonContent);
                        
                        _logger.LogDebug($"Signal marked as processed: {signalId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking signal as processed: {signalId}");
            }
        }
    }
} 