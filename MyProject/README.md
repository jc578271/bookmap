# Telegram Signal Bot for cTrader

This project creates a bridge between your Bookmap plugin (which sends signals to Telegram) and cTrader, allowing automatic trade execution based on Telegram signals.

## Architecture

The system consists of two main components:

1. **Telegram Signal Listener** - A .NET console application that:
   - Listens for messages from your Telegram bot
   - Parses trading signals from the messages
   - Saves signals to a JSON file

2. **cTrader Bot** - A cBot that:
   - Monitors the signal file for new signals
   - Executes trades based on the signals
   - Manages positions and risk

## Setup Instructions

### 1. Create a Telegram Bot

1. Message [@BotFather](https://t.me/botfather) on Telegram
2. Send `/newbot` and follow the instructions
3. Copy the bot token you receive

### 2. Configure the Application

1. Edit `appsettings.json`:
   ```json
   {
     "TelegramBot": {
       "Token": "YOUR_BOT_TOKEN_HERE",
       "AllowedChatIds": [123456789]  // Add your chat ID here
     }
   }
   ```

2. To get your chat ID:
   - Send a message to your bot
   - Visit: `https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates`
   - Look for the "chat" -> "id" field

### 3. Build and Run

#### Option A: Run as Console Application
```bash
cd MyProject
dotnet restore
dotnet run
```

#### Option B: Build for cTrader
```bash
cd MyProject/CTraderBot
dotnet build
```

### 4. Deploy to cTrader

1. Copy the compiled `TelegramSignalBot.dll` to your cTrader robots folder
2. In cTrader, add the bot to a chart
3. Configure the bot parameters:
   - **Signal File Path**: Path to the signals.json file (default: signals.json)
   - **Default Volume**: Default lot size for trades
   - **Default Stop Loss**: Default stop loss in pips
   - **Default Take Profit**: Default take profit in pips
   - **Max Positions**: Maximum number of concurrent positions
   - **Check Interval**: How often to check for new signals (in seconds)

## Signal Format

The bot recognizes signals in the following formats:

### Buy Signals
```
BUY EURUSD 1.0850 SL 1.0800 TP 1.0900 0.1 lot
LONG EURUSD at 1.0850
BUY_SIGNAL EURUSD entry 1.0850 stop 1.0800 target 1.0900
```

### Sell Signals
```
SELL GBPUSD 1.2500 SL 1.2550 TP 1.2450 0.1 lot
SHORT GBPUSD at 1.2500
SELL_SIGNAL GBPUSD entry 1.2500 stop 1.2550 target 1.2450
```

### Close Signals
```
CLOSE EURUSD
EXIT all positions
CLOSE_POSITION GBPUSD
```

## Configuration

### Signal Keywords
You can customize the keywords that trigger signals in `appsettings.json`:

```json
{
  "SignalSettings": {
    "SignalKeywords": {
      "Buy": ["BUY", "LONG", "BUY_SIGNAL", "GO_LONG"],
      "Sell": ["SELL", "SHORT", "SELL_SIGNAL", "GO_SHORT"],
      "Close": ["CLOSE", "EXIT", "CLOSE_POSITION", "STOP"]
    }
  }
}
```

### Trading Settings
```json
{
  "TradingSettings": {
    "DefaultVolume": 0.01,
    "DefaultStopLoss": 50,
    "DefaultTakeProfit": 100,
    "MaxPositions": 5,
    "RiskPercentage": 2.0
  }
}
```

## Security Features

- **Chat ID Whitelist**: Only messages from authorized chat IDs are processed
- **Signal Validation**: All signals are validated before processing
- **Error Handling**: Comprehensive error handling and logging
- **Position Limits**: Maximum position limits to prevent overexposure

## Logging

The application logs all activities to the console. You can configure logging levels in the code:

- **Information**: General bot activities
- **Warning**: Invalid signals or configuration issues
- **Error**: Failed trades or system errors
- **Debug**: Detailed signal parsing information

## Troubleshooting

### Bot Not Responding
1. Check if the bot token is correct
2. Verify the chat ID is in the allowed list
3. Ensure the bot is running and connected

### Signals Not Executing
1. Check if the signal file path is correct
2. Verify the symbol names match your broker's symbols
3. Check the cTrader bot logs for errors

### Invalid Signals
1. Review the signal format examples
2. Check the signal keywords configuration
3. Verify the message contains valid trading information

## File Structure

```
MyProject/
├── Program.cs                 # Main application entry point
├── appsettings.json          # Configuration file
├── TelegramSignalBot.csproj  # Project file
├── Models/
│   └── TradingSignal.cs      # Signal data model
├── Services/
│   ├── TelegramBotService.cs # Telegram bot listener
│   ├── SignalParser.cs       # Message parsing logic
│   ├── SignalProcessor.cs    # Signal processing
│   └── ISignalProcessor.cs   # Interface
└── CTraderBot/
    ├── TelegramSignalBot.cs  # cTrader bot implementation
    └── TelegramSignalBot.csproj
```

## Support

For issues or questions:
1. Check the logs for error messages
2. Verify your configuration settings
3. Test with simple signals first
4. Ensure your Bookmap plugin is sending messages in the expected format

## Disclaimer

This software is for educational purposes only. Trading involves risk and you should never risk more than you can afford to lose. Always test thoroughly on a demo account before using with real money. 