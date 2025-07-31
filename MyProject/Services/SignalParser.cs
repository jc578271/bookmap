using System.Text.RegularExpressions;
using TelegramSignalBot.Models;

namespace TelegramSignalBot.Services
{
    public class SignalParser
    {
        private readonly Dictionary<SignalType, string[]> _signalKeywords;

        public SignalParser(Dictionary<SignalType, string[]> signalKeywords)
        {
            _signalKeywords = signalKeywords;
        }

        public TradingSignal? ParseSignal(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            var signal = new TradingSignal
            {
                Message = message.Trim()
            };

            // Try to parse Bookmap/TTW style messages
            if (TryParseBookmapSignal(message, signal))
            {
                return signal;
            }

            // Fallback: Determine signal type by keywords
            signal.Type = DetermineSignalType(message);
            if (signal.Type == SignalType.Unknown)
                return null;

            // Extract symbol
            signal.Symbol = ExtractSymbol(message);

            // Extract price information
            var prices = ExtractPrices(message);
            if (prices.Count > 0)
            {
                signal.EntryPrice = prices.FirstOrDefault();
                if (prices.Count > 1) signal.StopLoss = prices[1];
                if (prices.Count > 2) signal.TakeProfit = prices[2];
            }

            // Extract volume
            signal.Volume = ExtractVolume(message);

            return signal;
        }

        /// <summary>
        /// Try to parse Bookmap/TTW plugin messages.
        /// </summary>
        private bool TryParseBookmapSignal(string message, TradingSignal signal)
        {
            // Instrument
            var instrumentMatch = Regex.Match(message, @"Instrument:\s*([\w\.]+)", RegexOptions.IgnoreCase);
            if (instrumentMatch.Success)
            {
                signal.Symbol = instrumentMatch.Groups[1].Value;
            }
            // Price
            var priceMatch = Regex.Match(message, @"Price:\s*([\d\.]+)", RegexOptions.IgnoreCase);
            if (priceMatch.Success && double.TryParse(priceMatch.Groups[1].Value, out var price))
            {
                signal.EntryPrice = price;
            }
            // Volume
            var volumeMatch = Regex.Match(message, @"Volume:\s*([\d\.]+)", RegexOptions.IgnoreCase);
            if (volumeMatch.Success && double.TryParse(volumeMatch.Groups[1].Value, out var volume))
            {
                signal.Volume = volume;
            }
            // Side
            var sideMatch = Regex.Match(message, @"Side:\s*(\w+)", RegexOptions.IgnoreCase);
            if (sideMatch.Success)
            {
                var side = sideMatch.Groups[1].Value.ToUpper();
                if (side == "ASK" || side == "SELL")
                    signal.Type = SignalType.Sell;
                else if (side == "BID" || side == "BUY")
                    signal.Type = SignalType.Buy;
                else
                    signal.Type = SignalType.Unknown;
            }
            // Direction
            var directionMatch = Regex.Match(message, @"Direction:\s*(\w+)", RegexOptions.IgnoreCase);
            if (directionMatch.Success)
            {
                var direction = directionMatch.Groups[1].Value.ToUpper();
                if (direction == "BUY" || direction == "UP")
                    signal.Type = SignalType.Buy;
                else if (direction == "SELL" || direction == "DOWN")
                    signal.Type = SignalType.Sell;
                else
                    signal.Type = SignalType.Unknown;
            }
            // Stop/Close
            if (message.ToUpper().Contains("STOP") || message.ToUpper().Contains("CLOSE"))
            {
                signal.Type = SignalType.Close;
            }
            // If we found at least one of instrument, price, or volume, treat as a valid Bookmap signal
            if (!string.IsNullOrEmpty(signal.Symbol) || signal.EntryPrice.HasValue || signal.Volume.HasValue)
            {
                return true;
            }
            return false;
        }

        private SignalType DetermineSignalType(string message)
        {
            var upperMessage = message.ToUpper();

            if (_signalKeywords[SignalType.Buy].Any(keyword => upperMessage.Contains(keyword)))
                return SignalType.Buy;

            if (_signalKeywords[SignalType.Sell].Any(keyword => upperMessage.Contains(keyword)))
                return SignalType.Sell;

            if (_signalKeywords[SignalType.Close].Any(keyword => upperMessage.Contains(keyword)))
                return SignalType.Close;

            return SignalType.Unknown;
        }

        private string ExtractSymbol(string message)
        {
            // Common forex pairs pattern
            var forexPattern = @"\b[A-Z]{6}\b";
            var match = Regex.Match(message.ToUpper(), forexPattern);
            if (match.Success)
                return match.Value;

            // Crypto pairs pattern
            var cryptoPattern = @"\b[A-Z]{3,4}/[A-Z]{3,4}\b";
            match = Regex.Match(message.ToUpper(), cryptoPattern);
            if (match.Success)
                return match.Value;

            // Index patterns
            var indexPattern = @"\b[A-Z]{2,5}INDEX\b";
            match = Regex.Match(message.ToUpper(), indexPattern);
            if (match.Success)
                return match.Value;

            return string.Empty;
        }

        private List<double> ExtractPrices(string message)
        {
            var prices = new List<double>();
            
            // Match decimal numbers (prices)
            var pricePattern = @"\b\d+\.?\d*\b";
            var matches = Regex.Matches(message, pricePattern);
            
            foreach (Match match in matches)
            {
                if (double.TryParse(match.Value, out double price))
                {
                    prices.Add(price);
                }
            }

            return prices;
        }

        private double? ExtractVolume(string message)
        {
            // Look for volume indicators like "0.1 lot" or "1000 units"
            var lotPattern = @"(\d+\.?\d*)\s*lot";
            var match = Regex.Match(message.ToLower(), lotPattern);
            if (match.Success && double.TryParse(match.Value.Split(' ')[0], out double volume))
                return volume;

            var unitPattern = @"(\d+)\s*unit";
            match = Regex.Match(message.ToLower(), unitPattern);
            if (match.Success && double.TryParse(match.Value.Split(' ')[0], out volume))
                return volume / 100000; // Convert to lots

            return null;
        }
    }
} 