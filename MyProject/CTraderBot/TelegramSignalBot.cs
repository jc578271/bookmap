using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using TelegramSignalBot.Models;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class TelegramSignalBot : Robot
    {
        [Parameter("Signal File Path", DefaultValue = "signals.json")]
        public string SignalFilePath { get; set; }

        [Parameter("Default Volume", DefaultValue = 0.01, MinValue = 0.01)]
        public double DefaultVolume { get; set; }

        [Parameter("Default Stop Loss (pips)", DefaultValue = 50, MinValue = 1)]
        public double DefaultStopLoss { get; set; }

        [Parameter("Default Take Profit (pips)", DefaultValue = 100, MinValue = 1)]
        public double DefaultTakeProfit { get; set; }

        [Parameter("Max Positions", DefaultValue = 5, MinValue = 1)]
        public int MaxPositions { get; set; }

        [Parameter("Check Interval (seconds)", DefaultValue = 5, MinValue = 1)]
        public int CheckInterval { get; set; }

        private DateTime _lastCheckTime = DateTime.MinValue;
        private readonly HashSet<string> _processedSignals = new();

        protected override void OnStart()
        {
            Print("Telegram Signal Bot started");
            Print($"Signal file path: {SignalFilePath}");
            Print($"Default volume: {DefaultVolume}");
            Print($"Default stop loss: {DefaultStopLoss} pips");
            Print($"Default take profit: {DefaultTakeProfit} pips");
            Print($"Max positions: {MaxPositions}");
            Print($"Check interval: {CheckInterval} seconds");
        }

        protected override void OnTick()
        {
            // Check for new signals every CheckInterval seconds
            if (DateTime.Now.Subtract(_lastCheckTime).TotalSeconds >= CheckInterval)
            {
                CheckForNewSignals();
                _lastCheckTime = DateTime.Now;
            }
        }

        private void CheckForNewSignals()
        {
            try
            {
                if (!File.Exists(SignalFilePath))
                {
                    return;
                }

                var jsonContent = File.ReadAllText(SignalFilePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    return;
                }

                var signals = JsonSerializer.Deserialize<List<TradingSignal>>(jsonContent);
                if (signals == null || !signals.Any())
                {
                    return;
                }

                // Process unprocessed signals
                var unprocessedSignals = signals.Where(s => !s.IsProcessed && !_processedSignals.Contains(s.Id)).ToList();
                
                foreach (var signal in unprocessedSignals)
                {
                    ProcessSignal(signal);
                    _processedSignals.Add(signal.Id);
                }

                // Mark signals as processed in the file
                MarkSignalsAsProcessed(signals, unprocessedSignals);
            }
            catch (Exception ex)
            {
                Print($"Error checking for signals: {ex.Message}");
            }
        }

        private void ProcessSignal(TradingSignal signal)
        {
            try
            {
                Print($"Processing signal: {signal}");

                switch (signal.Type)
                {
                    case SignalType.Buy:
                        ExecuteBuySignal(signal);
                        break;
                    case SignalType.Sell:
                        ExecuteSellSignal(signal);
                        break;
                    case SignalType.Close:
                        ExecuteCloseSignal(signal);
                        break;
                    default:
                        Print($"Unknown signal type: {signal.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Print($"Error processing signal {signal.Id}: {ex.Message}");
            }
        }

        private void ExecuteBuySignal(TradingSignal signal)
        {
            Print($"[LISTEN] Buy signal received: Symbol={signal.Symbol}, Price={signal.EntryPrice}, Volume={signal.Volume}, Message={signal.Message}");
            // TODO: Implement buy order execution logic here
        }

        private void ExecuteSellSignal(TradingSignal signal)
        {
            Print($"[LISTEN] Sell signal received: Symbol={signal.Symbol}, Price={signal.EntryPrice}, Volume={signal.Volume}, Message={signal.Message}");
            // TODO: Implement sell order execution logic here
        }

        private void ExecuteCloseSignal(TradingSignal signal)
        {
            Print($"[LISTEN] Close signal received: Symbol={signal.Symbol}, Message={signal.Message}");
            // TODO: Implement close position logic here
        }

        private Symbol? GetSymbol(string symbolName)
        {
            // Try to find exact match first
            var symbol = Symbols.FirstOrDefault(s => s.Name.Equals(symbolName, StringComparison.OrdinalIgnoreCase));
            if (symbol != null)
                return symbol;

            // Try common variations
            var variations = new[]
            {
                symbolName.Replace("/", ""),
                symbolName.Replace("INDEX", ""),
                symbolName + "INDEX"
            };

            foreach (var variation in variations)
            {
                symbol = Symbols.FirstOrDefault(s => s.Name.Equals(variation, StringComparison.OrdinalIgnoreCase));
                if (symbol != null)
                    return symbol;
            }

            return null;
        }

        private void MarkSignalsAsProcessed(List<TradingSignal> allSignals, List<TradingSignal> processedSignals)
        {
            try
            {
                foreach (var signal in processedSignals)
                {
                    var signalToUpdate = allSignals.FirstOrDefault(s => s.Id == signal.Id);
                    if (signalToUpdate != null)
                    {
                        signalToUpdate.IsProcessed = true;
                    }
                }

                var jsonContent = JsonSerializer.Serialize(allSignals, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SignalFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Print($"Error marking signals as processed: {ex.Message}");
            }
        }

        protected override void OnStop()
        {
            Print("Telegram Signal Bot stopped");
        }
    }
} 