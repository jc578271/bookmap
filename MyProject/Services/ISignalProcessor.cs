using TelegramSignalBot.Models;

namespace TelegramSignalBot.Services
{
    public interface ISignalProcessor
    {
        Task ProcessSignalAsync(TradingSignal signal);
    }
} 