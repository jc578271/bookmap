using TelegramSignalBot.Models;
using TelegramSignalBot.Services;

namespace TelegramSignalBot
{
    public class TestSignalParser
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Telegram Signal Parser Test");
            Console.WriteLine("===========================");

            // Initialize signal parser with default keywords
            var signalKeywords = new Dictionary<SignalType, string[]>
            {
                [SignalType.Buy] = new[] { "BUY", "LONG", "BUY_SIGNAL" },
                [SignalType.Sell] = new[] { "SELL", "SHORT", "SELL_SIGNAL" },
                [SignalType.Close] = new[] { "CLOSE", "EXIT", "CLOSE_POSITION" }
            };

            var parser = new SignalParser(signalKeywords);

            // Test signals
            var testSignals = new[]
            {
                "BUY EURUSD 1.0850 SL 1.0800 TP 1.0900 0.1 lot",
                "SELL GBPUSD 1.2500 SL 1.2550 TP 1.2450 0.1 lot",
                "LONG EURUSD at 1.0850",
                "SHORT GBPUSD at 1.2500",
                "CLOSE EURUSD",
                "EXIT all positions",
                "BUY_SIGNAL EURUSD entry 1.0850 stop 1.0800 target 1.0900",
                "SELL_SIGNAL GBPUSD entry 1.2500 stop 1.2550 target 1.2450",
                "Invalid message without signal",
                "BUY BTCUSD 45000 SL 44000 TP 46000 0.01 lot"
            };

            foreach (var testSignal in testSignals)
            {
                Console.WriteLine($"\nTesting: {testSignal}");
                Console.WriteLine("Result:");
                
                var signal = parser.ParseSignal(testSignal);
                if (signal != null)
                {
                    Console.WriteLine($"  Type: {signal.Type}");
                    Console.WriteLine($"  Symbol: {signal.Symbol}");
                    Console.WriteLine($"  Entry Price: {signal.EntryPrice}");
                    Console.WriteLine($"  Stop Loss: {signal.StopLoss}");
                    Console.WriteLine($"  Take Profit: {signal.TakeProfit}");
                    Console.WriteLine($"  Volume: {signal.Volume}");
                    Console.WriteLine($"  Message: {signal.Message}");
                }
                else
                {
                    Console.WriteLine("  No signal detected");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 