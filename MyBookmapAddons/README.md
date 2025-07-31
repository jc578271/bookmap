# Rithmic Connection Monitor for Bookmap

A Bookmap L1 addon that monitors Rithmic connection status and sends Telegram notifications when the connection is lost.

## Overview

This addon continuously monitors the data flow from Rithmic feed in Bookmap and detects when the connection is lost or interrupted. When a connection issue is detected, it automatically sends notifications to Telegram to alert you immediately.

## Features

- **Real-time Connection Monitoring**: Monitors depth, trade, and order data flow from Rithmic
- **Automatic Detection**: Detects connection loss based on data timeout (configurable)
- **Telegram Notifications**: Sends immediate alerts to Telegram when connection is lost
- **Bookmap Alerts**: Also shows popup and sound alerts within Bookmap
- **Configurable Settings**: Easy-to-use GUI for configuring Telegram bot settings
- **Connection Status Tracking**: Tracks connection status for each instrument separately

## Installation

### Prerequisites

- Bookmap 7.6.0 or later
- Java 17
- Gradle (for building)

### Building the Addon

#### Option 1: Using Gradle (Recommended)

1. Clone or download this project
2. Navigate to the project directory
3. Run the build command:
   ```bash
   gradle jar
   ```
4. The compiled JAR file will be created in `build/libs/bm-rithmic-connection-monitor.jar`

#### Option 2: Using Windows Batch File

1. Clone or download this project
2. Navigate to the project directory
3. Run the build script:
   ```cmd
   build.bat
   ```

#### Option 3: Manual Compilation

If you don't have Gradle installed:

1. Install Gradle from https://gradle.org/install/
2. Or use the manual compilation script:
   ```cmd
   compile.bat
   ```
   Note: Manual compilation requires setting up classpath with Bookmap API JARs and dependencies.

### Loading into Bookmap

1. Open Bookmap
2. Go to **Settings** → **Api plugins configuration**
3. Click **Add** and select the `bm-rithmic-connection-monitor.jar` file
4. Select **Rithmic Connection Monitor** from the list of available addons
5. Enable the addon using the checkbox

## Setup Instructions

### 1. Create a Telegram Bot

1. Open Telegram and search for [@BotFather](https://t.me/botfather)
2. Send `/newbot` and follow the instructions
3. Choose a name and username for your bot
4. Copy the bot token you receive (it looks like `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`)

### 2. Get Your Chat ID

1. Send a message to your newly created bot
2. Visit this URL in your browser (replace with your bot token):
   ```
   https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates
   ```
3. Look for the `"chat"` → `"id"` field in the response
4. Copy the chat ID (it's usually a number like `123456789`)

### 3. Configure the Addon

1. In Bookmap, go to **File** → **Alerts** → **Configure alerts**
2. Find **Rithmic Connection Monitor** in the list
3. Click **Configure alert** to open the settings panel
4. Enter your bot token and chat ID
5. Click **Save Settings**
6. Click **Test Connection** to verify everything works

## How It Works

### Connection Monitoring

The addon monitors several types of data flow from Rithmic:

- **Depth Data**: Order book updates
- **Trade Data**: Executed trades
- **Order Updates**: Order status changes
- **Instrument Events**: Instrument additions/removals

### Connection Loss Detection

The addon considers a connection lost when:

1. No data has been received for a configurable timeout period (default: 60 seconds)
2. This applies to each instrument separately
3. The timeout is measured from the last received data point

### Notification System

When a connection loss is detected:

1. **Bookmap Alert**: Shows a popup notification with sound
2. **Telegram Message**: Sends a formatted message to your Telegram chat
3. **Console Log**: Logs the event to Bookmap's console

## Configuration

### Telegram Settings

- **Bot Token**: Your Telegram bot's API token
- **Chat ID**: Your personal chat ID or group chat ID
- **Test Connection**: Verifies the Telegram configuration

### Monitoring Settings

- **Timeout Threshold**: How long to wait before considering connection lost (default: 60 seconds)
- **Monitoring Interval**: How often to check connection status (default: 30 seconds)

### Configuration File

You can also configure the addon using a properties file:

1. Copy `config-sample.properties` to `config.properties`
2. Edit the file with your settings:
   ```properties
   telegram.bot.token=YOUR_BOT_TOKEN_HERE
   telegram.chat.id=YOUR_CHAT_ID_HERE
   monitor.timeout.seconds=60
   monitor.check.interval.seconds=30
   ```
3. Place the config file in the same directory as the JAR file

## Message Format

### Telegram Notifications

When connection is lost:
```
⚠️ Rithmic connection lost for ES 12-24 at 2024-01-15T14:30:25
```

When connection is restored:
```
✅ Rithmic connection restored for ES 12-24 at 2024-01-15T14:35:10
```

### Bookmap Alerts

- **Popup**: Shows the connection status message
- **Sound**: Plays an alert sound (configurable repeats)
- **Log**: Records the event in Bookmap's console

## Troubleshooting

### Addon Not Loading

1. Check that you're using Bookmap 7.6.0 or later
2. Verify Java 17 is installed and configured
3. Check the Bookmap console for error messages
4. Ensure the JAR file was built correctly

### Telegram Notifications Not Working

1. Verify your bot token is correct
2. Check that your chat ID is correct
3. Ensure your bot is not blocked
4. Test the connection using the "Test Connection" button
5. Check that you've sent at least one message to your bot

### False Connection Loss Alerts

1. Increase the timeout threshold if you're getting false positives
2. Check your internet connection stability
3. Verify Rithmic feed is working properly
4. Monitor the console for any error messages

### Performance Issues

1. The addon uses minimal resources
2. Monitoring interval can be adjusted if needed
3. Check that you don't have too many instruments loaded

## Development

### Project Structure

```
MyBookmapAddons/
├── build.gradle                    # Build configuration
├── README.md                       # This file
└── src/main/java/com/bookmap/rithmicmonitor/
    ├── RithmicConnectionMonitor.java      # Main addon class
    ├── TelegramNotifier.java              # Telegram API integration
    ├── ConnectionMonitor.java             # Connection monitoring logic
    └── RithmicConnectionMonitorPanel.java # Settings GUI
```

### Building from Source

1. Ensure you have Java 17 and Gradle installed
2. Clone the repository
3. Run `gradle jar` to build
4. The JAR file will be in `build/libs/`

### Customization

You can modify the addon to:

- Change the timeout threshold
- Modify notification message format
- Add additional monitoring criteria
- Integrate with other notification services

## Security Notes

- **Bot Token**: Keep your bot token secure and don't share it
- **Chat ID**: Your chat ID is personal, but not highly sensitive
- **Network**: The addon makes HTTP requests to Telegram's API
- **Data**: No sensitive trading data is sent to Telegram

## Support

For issues or questions:

1. Check the Bookmap console for error messages
2. Verify your Telegram bot configuration
3. Test with a simple setup first
4. Check that Rithmic feed is working properly

## License

This project is provided as-is for educational and personal use. Use at your own risk.

## Disclaimer

This software is for educational purposes only. Trading involves risk and you should never risk more than you can afford to lose. Always test thoroughly before using with real trading systems. 