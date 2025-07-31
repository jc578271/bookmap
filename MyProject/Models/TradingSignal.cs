using System;

namespace TelegramSignalBot.Models
{
    public enum SignalType
    {
        Buy,
        Sell,
        Close,
        Unknown
    }

    public class TradingSignal
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SignalType Type { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public double? EntryPrice { get; set; }
        public double? StopLoss { get; set; }
        public double? TakeProfit { get; set; }
        public double? Volume { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = "Telegram";
        public bool IsProcessed { get; set; } = false;

        public override string ToString()
        {
            return $"{Type} {Symbol} - Entry: {EntryPrice}, SL: {StopLoss}, TP: {TakeProfit}, Volume: {Volume}";
        }
    }
} 